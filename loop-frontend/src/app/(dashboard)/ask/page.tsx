"use client";

import { useState } from "react";

export default function AskLoopPage() {
  const [question, setQuestion] = useState("");
  const [answer, setAnswer] = useState("");
  const [loading, setLoading] = useState(false);

  const handleAsk = async () => {
    if (!question.trim() || loading) {
      return;
    }

    setLoading(true);
    setAnswer("");

    try {
      const token = localStorage.getItem("token");

      if (!token) {
        setAnswer("Please login again.");
        return;
      }

      const response = await fetch(
        "https://loop-api-s464.onrender.com/api/AskLoop",
        {
          method: "POST",

          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`,
          },

          body: JSON.stringify({
            question: question.trim(),
          }),
        }
      );

      const data = await response
        .json()
        .catch(() => null);

      if (!response.ok) {
        console.error(
          "Ask LOOP API Error:",
          response.status,
          data
        );

        setAnswer(
          data?.message ||
            `Ask LOOP request failed (${response.status}).`
        );

        return;
      }

      setAnswer(
        data?.answer ||
          data?.response ||
          "No answer received."
      );
    } catch (error) {
      console.error(
        "Ask LOOP Error:",
        error
      );

      setAnswer(
        "Unable to connect to LOOP API. Please make sure the backend is running."
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 p-8">

      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">
          Ask LOOP
        </h1>

        <p className="mt-2 text-gray-500">
          Ask questions about your customer feedback.
        </p>
      </div>

      <div className="rounded-xl bg-white p-6 shadow-sm">

        <label className="mb-3 block text-sm font-semibold text-gray-700">
          Ask a question
        </label>

        <textarea
          value={question}
          onChange={(e) =>
            setQuestion(e.target.value)
          }
          disabled={loading}
          placeholder="Type your question about customer feedback..."
          className="min-h-[140px] w-full rounded-lg border border-gray-300 p-4 text-gray-900 outline-none focus:border-gray-500 disabled:bg-gray-100"
        />

        <div className="mt-4">

          <p className="mb-2 text-sm font-medium text-gray-600">
            Try asking:
          </p>

          <div className="flex flex-wrap gap-2">

            <button
              type="button"
              disabled={loading}
              onClick={() =>
                setQuestion(
                  "What are the main complaints from customers?"
                )
              }
              className="rounded-full border border-gray-300 px-4 py-2 text-sm text-gray-600 hover:bg-gray-100 disabled:opacity-50"
            >
              Main customer complaints?
            </button>

            <button
              type="button"
              disabled={loading}
              onClick={() =>
                setQuestion(
                  "What are customers most satisfied with?"
                )
              }
              className="rounded-full border border-gray-300 px-4 py-2 text-sm text-gray-600 hover:bg-gray-100 disabled:opacity-50"
            >
              What are customers satisfied with?
            </button>

            <button
              type="button"
              disabled={loading}
              onClick={() =>
                setQuestion(
                  "What are the most common negative feedback themes?"
                )
              }
              className="rounded-full border border-gray-300 px-4 py-2 text-sm text-gray-600 hover:bg-gray-100 disabled:opacity-50"
            >
              Common negative themes?
            </button>

            <button
              type="button"
              disabled={loading}
              onClick={() =>
                setQuestion(
                  "How do customers feel about our product?"
                )
              }
              className="rounded-full border border-gray-300 px-4 py-2 text-sm text-gray-600 hover:bg-gray-100 disabled:opacity-50"
            >
              Overall customer sentiment?
            </button>

          </div>
        </div>

        <div className="mt-5 flex justify-end">

          <button
            type="button"
            onClick={handleAsk}
            disabled={
              loading ||
              !question.trim()
            }
            className="rounded-lg bg-gray-900 px-6 py-3 font-medium text-white hover:bg-gray-800 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {loading
              ? "LOOP is analyzing..."
              : "Ask LOOP"}
          </button>

        </div>

        {loading && (
          <div className="mt-4 rounded-lg bg-gray-50 p-4">

            <p className="text-sm font-medium text-gray-700">
              LOOP is analyzing your feedback...
            </p>

            <p className="mt-1 text-xs text-gray-500">
              Please wait for the AI response.
            </p>

          </div>
        )}

      </div>

      {answer && (
        <div className="mt-6 rounded-xl bg-white p-6 shadow-sm">

          <h2 className="mb-4 text-lg font-semibold text-gray-900">
            LOOP's Answer
          </h2>

          <div className="whitespace-pre-wrap leading-7 text-gray-700">
            {answer}
          </div>

        </div>
      )}

    </div>
  );
}