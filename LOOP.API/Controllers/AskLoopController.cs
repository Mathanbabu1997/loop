using LOOP.API.Data;
using LOOP.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LOOP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AskLoopController : ControllerBase
    {
        private readonly LoopDbContext _context;
        private readonly IConfiguration _configuration;

        public AskLoopController(
            LoopDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // =========================================================
        // POST: api/AskLoop
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> Ask(
            [FromBody] AskLoopRequest request)
        {
            try
            {
                // =================================================
                // 1. VALIDATE QUESTION
                // =================================================

                if (string.IsNullOrWhiteSpace(request.Question))
                {
                    return BadRequest(new
                    {
                        message = "Question is required."
                    });
                }

                // =================================================
                // 2. GET WORKSPACE ID
                // =================================================

                var workspaceClaim =
                    User.FindFirst("workspaceId")?.Value;

                if (!Guid.TryParse(
                    workspaceClaim,
                    out var workspaceId))
                {
                    return Unauthorized(new
                    {
                        message = "Workspace not found."
                    });
                }

                // =================================================
                // 3. LOAD ALL FEEDBACK FOR WORKSPACE
                // =================================================

                var feedback = await _context.Feedbacks
                    .AsNoTracking()
                    .Where(f => f.WorkspaceId == workspaceId)
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(f => new
                    {
                        f.Content,
                        f.Channel,
                        Sentiment = f.Sentiment.ToString(),
                        f.SentimentScore,
                        f.CustomerLabel,
                        f.CreatedAt
                    })
                    .ToListAsync();

                // =================================================
                // 4. CHECK DATA
                // =================================================

                if (feedback.Count == 0)
                {
                    return Ok(new
                    {
                        answer =
                            "There is no customer feedback available yet.",

                        totalFeedback = 0
                    });
                }

                Console.WriteLine(
                    $"Ask LOOP -> Total feedback records loaded: {feedback.Count}"
                );

                // =================================================
                // 5. GROQ API KEY
                // =================================================

                var apiKey =
                    _configuration["Groq:ApiKey"];

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return StatusCode(500, new
                    {
                        message =
                            "Groq API key is not configured."
                    });
                }

                // =================================================
                // 6. GROQ MODEL
                // =================================================
                // Model is intentionally hardcoded.
                // No Groq:Model environment variable required.

                const string model =
                    "openai/gpt-oss-120b";

                // =================================================
                // 7. BATCH SETTINGS
                // =================================================

                const int batchSize = 30;

                var batchAnalyses =
                    new List<string>();

                // =================================================
                // 8. HTTP CLIENT
                // =================================================

                using var client = new HttpClient();

                client.Timeout =
                    TimeSpan.FromMinutes(5);

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        apiKey
                    );

                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue(
                        "application/json"
                    )
                );

                // =================================================
                // 9. CALCULATE BATCHES
                // =================================================

                int totalBatches =
                    (int)Math.Ceiling(
                        feedback.Count /
                        (double)batchSize
                    );

                Console.WriteLine(
                    $"Ask LOOP -> Processing {totalBatches} batches of {batchSize} records."
                );

                // =================================================
                // 10. PROCESS BATCHES
                // =================================================

                for (
                    int batchNumber = 0;
                    batchNumber < totalBatches;
                    batchNumber++
                )
                {
                    int skip =
                        batchNumber * batchSize;

                    var batch =
                        feedback
                            .Skip(skip)
                            .Take(batchSize)
                            .ToList();

                    Console.WriteLine(
                        $"Ask LOOP -> Processing batch " +
                        $"{batchNumber + 1}/{totalBatches} " +
                        $"with {batch.Count} records."
                    );

                    // =================================================
                    // BUILD BATCH TEXT
                    // =================================================

                    var batchBuilder =
                        new StringBuilder();

                    for (int i = 0; i < batch.Count; i++)
                    {
                        var f = batch[i];

                        batchBuilder.AppendLine(
                            $"#{skip + i + 1} | " +
                            $"Content: {f.Content} | " +
                            $"Channel: {f.Channel} | " +
                            $"Sentiment: {f.Sentiment} | " +
                            $"Score: {f.SentimentScore} | " +
                            $"Customer: {f.CustomerLabel} | " +
                            $"Date: {f.CreatedAt:yyyy-MM-dd}"
                        );
                    }

                    var batchText =
                        batchBuilder.ToString();

                    // =================================================
                    // BATCH PROMPT
                    // =================================================

                    var batchPrompt = $"""
You are LOOP, a customer feedback intelligence assistant.

Analyze ONLY the customer feedback in this batch.

The user asked:

{request.Question}

Rules:

- Use ONLY the feedback provided below.
- Do not invent information.
- Do not make assumptions outside the feedback.
- Identify information relevant to the user's question.
- Mention exact counts when possible.
- Identify repeated complaints, positive points, themes,
  sentiment patterns, or other relevant evidence.
- Keep the response concise.
- This is an intermediate analysis.
- Do NOT say that this is the final answer.
- Preserve important facts that may be needed for the final answer.

Batch:
{batchNumber + 1} of {totalBatches}

Records in this batch:
{batch.Count}

CUSTOMER FEEDBACK:

{batchText}
""";

                    // =================================================
                    // GROQ REQUEST BODY
                    // =================================================

                    var batchRequestBody = new
                    {
                        model = model,

                        messages = new object[]
                        {
                            new
                            {
                                role = "system",

                                content =
                                    "You are LOOP, a customer feedback " +
                                    "intelligence assistant. " +
                                    "Analyze only the supplied customer feedback."
                            },

                            new
                            {
                                role = "user",

                                content = batchPrompt
                            }
                        },

                        temperature = 0.2,

                        max_completion_tokens = 1000,

                        reasoning_effort = "low"
                    };

                    var batchJson =
                        JsonSerializer.Serialize(
                            batchRequestBody
                        );

                    Console.WriteLine(
                        $"Ask LOOP -> Batch " +
                        $"{batchNumber + 1} request size: " +
                        $"{batchJson.Length} characters"
                    );

                    // =================================================
                    // CALL GROQ
                    // =================================================

                    var batchResult =
                        await CallGroqAsync(
                            client,
                            batchJson
                        );

                    // =================================================
                    // CHECK GROQ RESULT
                    // =================================================

                    if (!batchResult.Success)
                    {
                        return StatusCode(
                            batchResult.StatusCode,
                            new
                            {
                                message =
                                    batchResult.Message,

                                batch =
                                    batchNumber + 1,

                                totalBatches =
                                    totalBatches,

                                totalFeedback =
                                    feedback.Count,

                                groqError =
                                    batchResult.Response
                            }
                        );
                    }

                    if (
                        string.IsNullOrWhiteSpace(
                            batchResult.Answer
                        )
                    )
                    {
                        return StatusCode(502, new
                        {
                            message =
                                "Groq returned an empty batch analysis.",

                            batch =
                                batchNumber + 1
                        });
                    }

                    batchAnalyses.Add(
                        $"BATCH {batchNumber + 1}:\n" +
                        batchResult.Answer
                    );

                    Console.WriteLine(
                        $"Ask LOOP -> Batch " +
                        $"{batchNumber + 1}/{totalBatches} completed."
                    );
                }

                // =================================================
                // 11. COMBINE BATCH ANALYSES
                // =================================================

                var analysisBuilder =
                    new StringBuilder();

                foreach (var analysis in batchAnalyses)
                {
                    analysisBuilder.AppendLine(
                        analysis
                    );

                    analysisBuilder.AppendLine();
                }

                var combinedAnalysis =
                    analysisBuilder.ToString();

                Console.WriteLine(
                    $"Ask LOOP -> Combined analysis characters: " +
                    $"{combinedAnalysis.Length}"
                );

                // =================================================
                // 12. FINAL PROMPT
                // =================================================

                var finalPrompt = $"""
You are LOOP, a customer feedback intelligence assistant.

The workspace contains {feedback.Count} customer feedback records.

The feedback was processed in multiple batches because sending
all records in one request can exceed the AI request size.

The following are factual analyses from ALL batches.

USER QUESTION:

{request.Question}

BATCH ANALYSES:

{combinedAnalysis}

IMPORTANT RULES:

- Answer the user's question using only the information contained
  in the batch analyses.
- The batch analyses represent the complete workspace feedback.
- Do not invent information.
- Do not make assumptions.
- Give a clear and concise business answer.
- When useful, mention counts or percentages.
- If the user asks about complaints, focus on negative feedback.
- If the user asks about satisfaction, focus on positive feedback.
- If the user asks about themes, identify repeated patterns.
- If the user asks about sentiment, use the sentiment information.
- If there is not enough information, say:
  "There is not enough feedback data to answer this."
- Do not mention internal batching unless the user asks about it.

Give the final answer now.
""";

                // =================================================
                // 13. FINAL REQUEST BODY
                // =================================================

                var finalRequestBody = new
                {
                    model = model,

                    messages = new object[]
                    {
                        new
                        {
                            role = "system",

                            content =
                                "You are LOOP, a customer feedback " +
                                "intelligence assistant. " +
                                "Only use the supplied customer feedback analysis."
                        },

                        new
                        {
                            role = "user",

                            content = finalPrompt
                        }
                    },

                    temperature = 0.2,

                    max_completion_tokens = 1500,

                    reasoning_effort = "low"
                };

                var finalJson =
                    JsonSerializer.Serialize(
                        finalRequestBody
                    );

                Console.WriteLine(
                    $"Ask LOOP -> Final request size: " +
                    $"{finalJson.Length} characters"
                );

                // =================================================
                // 14. FINAL GROQ CALL
                // =================================================

                var finalResult =
                    await CallGroqAsync(
                        client,
                        finalJson
                    );

                if (!finalResult.Success)
                {
                    return StatusCode(
                        finalResult.StatusCode,
                        new
                        {
                            message =
                                finalResult.Message,

                            totalFeedback =
                                feedback.Count,

                            totalBatches =
                                totalBatches,

                            groqError =
                                finalResult.Response
                        }
                    );
                }

                if (
                    string.IsNullOrWhiteSpace(
                        finalResult.Answer
                    )
                )
                {
                    return StatusCode(502, new
                    {
                        message =
                            "Groq did not generate a final answer.",

                        totalFeedback =
                            feedback.Count
                    });
                }

                // =================================================
                // 15. SUCCESS
                // =================================================

                Console.WriteLine(
                    "Ask LOOP -> Final answer generated successfully."
                );

                return Ok(new
                {
                    answer =
                        finalResult.Answer,

                    totalFeedback =
                        feedback.Count,

                    totalBatches =
                        totalBatches
                });
            }

            // =========================================================
            // JSON ERROR
            // =========================================================

            catch (JsonException ex)
            {
                Console.WriteLine(
                    $"JSON error: {ex.Message}"
                );

                return StatusCode(502, new
                {
                    message =
                        "Groq returned an invalid response.",

                    error =
                        ex.Message
                });
            }

            // =========================================================
            // TIMEOUT
            // =========================================================

            catch (TaskCanceledException ex)
            {
                Console.WriteLine(
                    $"Groq request cancelled: {ex.Message}"
                );

                return StatusCode(504, new
                {
                    message =
                        "The Groq request timed out or was cancelled.",

                    error =
                        ex.Message
                });
            }

            // =========================================================
            // GENERAL ERROR
            // =========================================================

            catch (Exception ex)
            {
                Console.WriteLine(
                    "=========================================="
                );

                Console.WriteLine(
                    $"Ask LOOP ERROR: {ex}"
                );

                Console.WriteLine(
                    "=========================================="
                );

                return StatusCode(500, new
                {
                    message =
                        "Something went wrong while processing your question.",

                    error =
                        ex.Message
                });
            }
        }

        // =========================================================
        // GROQ HELPER
        // =========================================================

        private async Task<GroqResult> CallGroqAsync(
            HttpClient client,
            string json)
        {
            const int maxAttempts = 3;

            for (
                int attempt = 1;
                attempt <= maxAttempts;
                attempt++)
            {
                try
                {
                    using var content =
                        new StringContent(
                            json,
                            Encoding.UTF8,
                            "application/json"
                        );

                    Console.WriteLine(
                        $"Ask LOOP -> Groq attempt " +
                        $"{attempt}/{maxAttempts}"
                    );

                    var response =
                        await client.PostAsync(
                            "https://api.groq.com/openai/v1/chat/completions",
                            content
                        );

                    var responseText =
                        await response.Content.ReadAsStringAsync();

                    Console.WriteLine(
                        "=========================================="
                    );

                    Console.WriteLine(
                        $"GROQ STATUS: {(int)response.StatusCode}"
                    );

                    Console.WriteLine(
                        $"GROQ RESPONSE: {responseText}"
                    );

                    Console.WriteLine(
                        "=========================================="
                    );

                    // =================================================
                    // SUCCESS
                    // =================================================

                    if (response.IsSuccessStatusCode)
                    {
                        using var document =
                            JsonDocument.Parse(
                                responseText
                            );

                        var root =
                            document.RootElement;

                        // -------------------------------------------------
                        // choices
                        // -------------------------------------------------

                        if (
                            !root.TryGetProperty(
                                "choices",
                                out var choices
                            )
                        )
                        {
                            return new GroqResult
                            {
                                Success = false,
                                StatusCode = 502,
                                Message =
                                    "Groq returned an unexpected response: choices missing.",
                                Response =
                                    responseText
                            };
                        }

                        if (
                            choices.ValueKind != JsonValueKind.Array ||
                            choices.GetArrayLength() == 0
                        )
                        {
                            return new GroqResult
                            {
                                Success = false,
                                StatusCode = 502,
                                Message =
                                    "Groq did not generate an answer.",
                                Response =
                                    responseText
                            };
                        }

                        var firstChoice =
                            choices[0];

                        // -------------------------------------------------
                        // message
                        // -------------------------------------------------

                        if (
                            !firstChoice.TryGetProperty(
                                "message",
                                out var message
                            )
                        )
                        {
                            return new GroqResult
                            {
                                Success = false,
                                StatusCode = 502,
                                Message =
                                    "Groq response does not contain a message.",
                                Response =
                                    responseText
                            };
                        }

                        // -------------------------------------------------
                        // content
                        // -------------------------------------------------

                        string? answer = null;

                        if (
                            message.TryGetProperty(
                                "content",
                                out var contentElement
                            )
                        )
                        {
                            if (
                                contentElement.ValueKind ==
                                JsonValueKind.String
                            )
                            {
                                answer =
                                    contentElement.GetString();
                            }
                        }

                        if (
                            string.IsNullOrWhiteSpace(
                                answer
                            )
                        )
                        {
                            // Some reasoning responses may expose
                            // useful text in reasoning_content.
                            if (
                                message.TryGetProperty(
                                    "reasoning_content",
                                    out var reasoningElement
                                )
                                &&
                                reasoningElement.ValueKind ==
                                JsonValueKind.String
                            )
                            {
                                var reasoning =
                                    reasoningElement.GetString();

                                if (
                                    !string.IsNullOrWhiteSpace(
                                        reasoning
                                    )
                                )
                                {
                                    Console.WriteLine(
                                        "Groq content was empty, but reasoning_content was returned."
                                    );

                                    answer =
                                        reasoning;
                                }
                            }
                        }

                        if (
                            string.IsNullOrWhiteSpace(
                                answer
                            )
                        )
                        {
                            return new GroqResult
                            {
                                Success = false,
                                StatusCode = 502,
                                Message =
                                    "Groq returned an empty answer.",
                                Response =
                                    responseText
                            };
                        }

                        return new GroqResult
                        {
                            Success = true,
                            StatusCode = 200,
                            Message = "Success",
                            Answer = answer,
                            Response = responseText
                        };
                    }

                    // =================================================
                    // 429 - RATE LIMIT
                    // =================================================

                    if (
                        response.StatusCode ==
                        HttpStatusCode.TooManyRequests
                    )
                    {
                        if (attempt < maxAttempts)
                        {
                            int delaySeconds = 5;

                            if (
                                response.Headers.RetryAfter != null
                                &&
                                response.Headers.RetryAfter.Delta.HasValue
                            )
                            {
                                delaySeconds =
                                    Math.Max(
                                        1,
                                        (int)
                                            response
                                                .Headers
                                                .RetryAfter
                                                .Delta
                                                .Value
                                                .TotalSeconds
                                    );
                            }

                            Console.WriteLine(
                                $"Groq rate limit reached. " +
                                $"Waiting {delaySeconds} seconds..."
                            );

                            await Task.Delay(
                                TimeSpan.FromSeconds(
                                    delaySeconds
                                )
                            );

                            continue;
                        }

                        return new GroqResult
                        {
                            Success = false,
                            StatusCode = 429,
                            Message =
                                "Groq API rate limit reached.",
                            Response =
                                responseText
                        };
                    }

                    // =================================================
                    // 413 - REQUEST TOO LARGE
                    // =================================================

                    if (
                        response.StatusCode ==
                        HttpStatusCode.RequestEntityTooLarge
                    )
                    {
                        return new GroqResult
                        {
                            Success = false,
                            StatusCode = 413,
                            Message =
                                "The Groq request is too large.",
                            Response =
                                responseText
                        };
                    }

                    // =================================================
                    // 400 - BAD REQUEST
                    // =================================================

                    if (
                        response.StatusCode ==
                        HttpStatusCode.BadRequest
                    )
                    {
                        return new GroqResult
                        {
                            Success = false,
                            StatusCode = 400,
                            Message =
                                "Groq rejected the request.",
                            Response =
                                responseText
                        };
                    }

                    // =================================================
                    // 401 - INVALID API KEY
                    // =================================================

                    if (
                        response.StatusCode ==
                        HttpStatusCode.Unauthorized
                    )
                    {
                        return new GroqResult
                        {
                            Success = false,
                            StatusCode = 401,
                            Message =
                                "Groq API key is invalid or unauthorized.",
                            Response =
                                responseText
                        };
                    }

                    // =================================================
                    // 403 - FORBIDDEN
                    // =================================================

                    if (
                        response.StatusCode ==
                        HttpStatusCode.Forbidden
                    )
                    {
                        return new GroqResult
                        {
                            Success = false,
                            StatusCode = 403,
                            Message =
                                "Groq denied access to this request.",
                            Response =
                                responseText
                        };
                    }

                    // =================================================
                    // 404 - MODEL / ENDPOINT
                    // =================================================

                    if (
                        response.StatusCode ==
                        HttpStatusCode.NotFound
                    )
                    {
                        return new GroqResult
                        {
                            Success = false,
                            StatusCode = 404,
                            Message =
                                "Groq model or endpoint was not found.",
                            Response =
                                responseText
                        };
                    }

                    // =================================================
                    // 500 / 502 / 503
                    // =================================================

                    if (
                        response.StatusCode ==
                            HttpStatusCode.InternalServerError
                        ||
                        response.StatusCode ==
                            HttpStatusCode.BadGateway
                        ||
                        response.StatusCode ==
                            HttpStatusCode.ServiceUnavailable
                    )
                    {
                        if (attempt < maxAttempts)
                        {
                            var delaySeconds =
                                3 * Math.Pow(
                                    2,
                                    attempt - 1
                                );

                            Console.WriteLine(
                                $"Groq server error. " +
                                $"Retrying after {delaySeconds} seconds..."
                            );

                            await Task.Delay(
                                TimeSpan.FromSeconds(
                                    delaySeconds
                                )
                            );

                            continue;
                        }

                        return new GroqResult
                        {
                            Success = false,
                            StatusCode =
                                (int)response.StatusCode,
                            Message =
                                "Groq AI service is temporarily unavailable.",
                            Response =
                                responseText
                        };
                    }

                    // =================================================
                    // OTHER ERROR
                    // =================================================

                    return new GroqResult
                    {
                        Success = false,
                        StatusCode =
                            (int)response.StatusCode,
                        Message =
                            "Groq could not process the request.",
                        Response =
                            responseText
                    };
                }

                // =====================================================
                // HTTP REQUEST ERROR
                // =====================================================

                catch (HttpRequestException ex)
                {
                    Console.WriteLine(
                        $"Groq HTTP error: {ex.Message}"
                    );

                    if (attempt < maxAttempts)
                    {
                        var delaySeconds =
                            3 * Math.Pow(
                                2,
                                attempt - 1
                            );

                        await Task.Delay(
                            TimeSpan.FromSeconds(
                                delaySeconds
                            )
                        );

                        continue;
                    }

                    return new GroqResult
                    {
                        Success = false,
                        StatusCode = 503,
                        Message =
                            "Unable to connect to Groq API.",
                        Response =
                            ex.Message
                    };
                }
            }

            return new GroqResult
            {
                Success = false,
                StatusCode = 503,
                Message =
                    "Groq AI is temporarily unavailable.",
                Response = ""
            };
        }

        // =========================================================
        // GROQ RESULT CLASS
        // =========================================================

        private class GroqResult
        {
            public bool Success { get; set; }

            public int StatusCode { get; set; }

            public string Message { get; set; } = "";

            public string Answer { get; set; } = "";

            public string Response { get; set; } = "";
        }
    }
}