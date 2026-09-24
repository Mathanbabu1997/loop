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
                // 3. GET ALL FEEDBACK
                // =================================================
                // IMPORTANT:
                // No Take(30)
                //
                // If you have 110 records,
                // all 110 records will be loaded.
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
                            "There is no customer feedback available yet."
                    });
                }


                Console.WriteLine(
                    $"Ask LOOP -> Total feedback records: {feedback.Count}"
                );


                // =================================================
                // 5. BUILD COMPACT FEEDBACK TEXT
                // =================================================
                //
                // We keep all records but remove unnecessary labels
                // to reduce request size.
                // =================================================

                var feedbackBuilder =
                    new StringBuilder();

                for (int i = 0; i < feedback.Count; i++)
                {
                    var f = feedback[i];

                    feedbackBuilder.AppendLine(
                        $"#{i + 1} | " +
                        $"Content: {f.Content} | " +
                        $"Channel: {f.Channel} | " +
                        $"Sentiment: {f.Sentiment} | " +
                        $"Score: {f.SentimentScore} | " +
                        $"Customer: {f.CustomerLabel} | " +
                        $"Date: {f.CreatedAt:yyyy-MM-dd}"
                    );
                }

                var feedbackText =
                    feedbackBuilder.ToString();


                Console.WriteLine(
                    $"Ask LOOP -> Feedback characters: {feedbackText.Length}"
                );


                // =================================================
                // 6. GROQ API KEY
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
                // 7. GROQ MODEL
                // =================================================

                var model =
                    _configuration["Groq:Model"];

                if (string.IsNullOrWhiteSpace(model))
                {
                    model = "openai/gpt-oss-120b";
                }


                // =================================================
                // 8. PROMPT
                // =================================================

                var prompt = $"""
                You are LOOP, a customer feedback intelligence assistant.

                Analyze the customer feedback provided below.

                IMPORTANT RULES:

                - Answer the user's question using ONLY the provided
                  customer feedback.
                - Do not invent information.
                - Do not make assumptions outside the feedback.
                - If there is not enough information, say:
                  "There is not enough feedback data to answer this."
                - Give a clear and concise business answer.
                - When useful, mention counts or percentages.
                - If the user asks about complaints, focus on negative
                  feedback.
                - If the user asks about satisfaction, focus on positive
                  feedback.
                - If the user asks about themes, identify repeated
                  patterns.
                - If the user asks about sentiment, analyze the
                  Sentiment field.
                - Consider ALL feedback records below.

                Total feedback records: {feedback.Count}

                USER QUESTION:
                {request.Question}

                CUSTOMER FEEDBACK:
                {feedbackText}
                """;


                // =================================================
                // 9. REQUEST BODY
                // =================================================

                var requestBody = new
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
                                "Only use the customer feedback supplied " +
                                "in the user message."
                        },

                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    },

                    temperature = 0.2,

                    max_tokens = 500
                };


                var json =
                    JsonSerializer.Serialize(requestBody);


                Console.WriteLine(
                    $"Ask LOOP -> Request size: {json.Length} characters"
                );


                // =================================================
                // 10. HTTP CLIENT
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
                // 11. RETRY
                // =================================================

                const int maxAttempts = 3;


                // =================================================
                // 12. CALL GROQ
                // =================================================

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


                        // =================================================
                        // DEBUG
                        // =================================================

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
                                JsonDocument.Parse(responseText);


                            if (!document.RootElement.TryGetProperty(
                                "choices",
                                out var choices))
                            {
                                return StatusCode(502, new
                                {
                                    message =
                                        "Groq returned an unexpected response.",

                                    groqResponse =
                                        responseText
                                });
                            }


                            if (choices.GetArrayLength() == 0)
                            {
                                return StatusCode(502, new
                                {
                                    message =
                                        "Groq did not generate an answer.",

                                    groqResponse =
                                        responseText
                                });
                            }


                            var answer =
                                choices[0]
                                    .GetProperty("message")
                                    .GetProperty("content")
                                    .GetString();


                            if (string.IsNullOrWhiteSpace(answer))
                            {
                                return StatusCode(502, new
                                {
                                    message =
                                        "Groq returned an empty answer.",

                                    groqResponse =
                                        responseText
                                });
                            }


                            return Ok(new
                            {
                                answer = answer,

                                totalFeedback =
                                    feedback.Count
                            });
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
                                            (int)response
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


                            return StatusCode(429, new
                            {
                                message =
                                    "Groq API rate limit reached.",

                                statusCode =
                                    (int)response.StatusCode,

                                groqError =
                                    responseText
                            });
                        }


                        // =================================================
                        // 413 - REQUEST TOO LARGE
                        // =================================================

                        if (
                            response.StatusCode ==
                            HttpStatusCode.RequestEntityTooLarge
                        )
                        {
                            return StatusCode(413, new
                            {
                                message =
                                    "The customer feedback data is too large for one Groq request.",

                                statusCode =
                                    (int)response.StatusCode,

                                totalFeedback =
                                    feedback.Count,

                                requestCharacters =
                                    json.Length,

                                groqError =
                                    responseText
                            });
                        }


                        // =================================================
                        // 400 - BAD REQUEST
                        // =================================================

                        if (
                            response.StatusCode ==
                            HttpStatusCode.BadRequest
                        )
                        {
                            return StatusCode(400, new
                            {
                                message =
                                    "Groq rejected the request.",

                                statusCode =
                                    (int)response.StatusCode,

                                groqError =
                                    responseText
                            });
                        }


                        // =================================================
                        // 401 - INVALID API KEY
                        // =================================================

                        if (
                            response.StatusCode ==
                            HttpStatusCode.Unauthorized
                        )
                        {
                            return StatusCode(401, new
                            {
                                message =
                                    "Groq API key is invalid or unauthorized.",

                                statusCode =
                                    (int)response.StatusCode,

                                groqError =
                                    responseText
                            });
                        }


                        // =================================================
                        // 403 - FORBIDDEN
                        // =================================================

                        if (
                            response.StatusCode ==
                            HttpStatusCode.Forbidden
                        )
                        {
                            return StatusCode(403, new
                            {
                                message =
                                    "Groq denied access to this request.",

                                statusCode =
                                    (int)response.StatusCode,

                                groqError =
                                    responseText
                            });
                        }


                        // =================================================
                        // 404 - MODEL / ENDPOINT
                        // =================================================

                        if (
                            response.StatusCode ==
                            HttpStatusCode.NotFound
                        )
                        {
                            return StatusCode(404, new
                            {
                                message =
                                    "Groq model or endpoint was not found.",

                                statusCode =
                                    (int)response.StatusCode,

                                model =
                                    model,

                                groqError =
                                    responseText
                            });
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


                            return StatusCode(
                                (int)response.StatusCode,
                                new
                                {
                                    message =
                                        "Groq AI service is temporarily unavailable.",

                                    statusCode =
                                        (int)response.StatusCode,

                                    groqError =
                                        responseText
                                }
                            );
                        }


                        // =================================================
                        // OTHER ERROR
                        // =================================================

                        return StatusCode(
                            (int)response.StatusCode,
                            new
                            {
                                message =
                                    "Groq could not process the question.",

                                statusCode =
                                    (int)response.StatusCode,

                                groqError =
                                    responseText
                            }
                        );
                    }


                    // =================================================
                    // HTTP REQUEST ERROR
                    // =================================================

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


                        return StatusCode(503, new
                        {
                            message =
                                "Unable to connect to Groq API.",

                            error =
                                ex.Message
                        });
                    }
                }


                // =================================================
                // FINAL FALLBACK
                // =================================================

                return StatusCode(503, new
                {
                    message =
                        "Groq AI is temporarily unavailable."
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
            // REQUEST CANCELLED / TIMEOUT
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
    }
}