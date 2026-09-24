"use client";

import { useEffect, useState } from "react";
import jsPDF from "jspdf";
import autoTable from "jspdf-autotable";

type ThemeSummary = {
  name: string;
  count: number;
  percentage: number;
};

type ChannelSummary = {
  name: string;
  count: number;
  percentage: number;
};

type CustomerQuote = {
  content: string;
  sentiment: string;
  customer: string;
  channel: string;
  date: string;
};

type ReportContent = {
  summary: string;

  totalFeedback: number;

  sentiment: {
    positive: number;
    neutral: number;
    negative: number;
    positivePercentage: number;
    neutralPercentage: number;
    negativePercentage: number;
  };

  sentimentShift: {
    positive: number;
    neutral: number;
    negative: number;
  };

  topThemes: ThemeSummary[];

  channels: ChannelSummary[];

  quotes: CustomerQuote[];

  recommendedActions: string[];
};

type ReportData = {
  id: string;
  title: string;
  periodStart: string;
  periodEnd: string;
  createdAt: string;
  content: ReportContent;
};

type SavedReport = {
  id: string;
  title: string;
  periodStart: string;
  periodEnd: string;
  createdAt: string;
};

export default function ReportsPage() {
  const [report, setReport] =
    useState<ReportData | null>(null);

  const [savedReports, setSavedReports] =
    useState<SavedReport[]>([]);

  const [startDate, setStartDate] =
    useState("");

  const [endDate, setEndDate] =
    useState("");

  const [period, setPeriod] =
    useState("30");

  const [loading, setLoading] =
    useState(false);

  const [loadingSaved, setLoadingSaved] =
    useState(true);

  const [error, setError] =
    useState("");

  const [showSaved, setShowSaved] =
    useState(false);

  // =========================================================
  // INITIAL LOAD
  // =========================================================

  useEffect(() => {
    setDateRange("30");
    loadSavedReports();
  }, []);

  // =========================================================
  // DATE RANGE
  // =========================================================

  const setDateRange = (value: string) => {
    setPeriod(value);

    const end = new Date();
    const start = new Date();

    if (value === "7") {
      start.setDate(end.getDate() - 6);
    }

    if (value === "30") {
      start.setDate(end.getDate() - 29);
    }

    if (value === "90") {
      start.setDate(end.getDate() - 89);
    }

    if (
      value !== "7" &&
      value !== "30" &&
      value !== "90"
    ) {
      return;
    }

    setStartDate(formatDate(start));
    setEndDate(formatDate(end));
  };

  const formatDate = (date: Date) => {
    const year = date.getFullYear();

    const month = String(
      date.getMonth() + 1
    ).padStart(2, "0");

    const day = String(
      date.getDate()
    ).padStart(2, "0");

    return `${year}-${month}-${day}`;
  };

  const formatDisplayDate = (value: string) => {
    return new Date(value).toLocaleDateString(
      "en-IN"
    );
  };

  // =========================================================
  // LOAD SAVED REPORTS
  // =========================================================

  const loadSavedReports = async () => {
    try {
      setLoadingSaved(true);

      const token =
        localStorage.getItem("token");

      if (!token) {
        return;
      }

      const response = await fetch(
        "https://localhost:7248/api/Report/saved",
        {
          method: "GET",
          headers: {
            Authorization:
              `Bearer ${token}`,
          },
        }
      );

      const data =
        await response
          .json()
          .catch(() => null);

      if (!response.ok) {
        return;
      }

      setSavedReports(data || []);
    } catch (error) {
      console.error(
        "Saved reports error:",
        error
      );
    } finally {
      setLoadingSaved(false);
    }
  };

  // =========================================================
  // GENERATE REPORT
  // =========================================================

  const generateReport = async () => {
    if (!startDate || !endDate) {
      setError(
        "Please select a report period."
      );

      return;
    }

    try {
      setLoading(true);
      setError("");
      setReport(null);

      const token =
        localStorage.getItem("token");

      if (!token) {
        setError(
          "Please login again."
        );

        return;
      }

      const response = await fetch(
        "https://localhost:7248/api/Report/generate",
        {
          method: "POST",

          headers: {
            "Content-Type":
              "application/json",

            Authorization:
              `Bearer ${token}`,
          },

          body: JSON.stringify({
            startDate:
              `${startDate}T00:00:00`,

            endDate:
              `${endDate}T00:00:00`,
          }),
        }
      );

      const data =
        await response
          .json()
          .catch(() => null);

      if (!response.ok) {
  console.error("Report API error:", data);

  setError(
    data?.error ||
      data?.message ||
      `Unable to generate report (${response.status}).`
  );

  return;
}

      setReport(data);

      await loadSavedReports();
    } catch (error) {
      console.error(
        "Generate report error:",
        error
      );

      setError(
        "Unable to connect to LOOP API. Please make sure the backend is running."
      );
    } finally {
      setLoading(false);
    }
  };

  // =========================================================
  // VIEW SAVED REPORT
  // =========================================================

  const viewSavedReport = async (
    id: string
  ) => {
    try {
      setLoading(true);
      setError("");

      const token =
        localStorage.getItem("token");

      if (!token) {
        setError(
          "Please login again."
        );

        return;
      }

      const response = await fetch(
        `https://localhost:7248/api/Report/${id}`,
        {
          method: "GET",

          headers: {
            Authorization:
              `Bearer ${token}`,
          },
        }
      );

      const data =
        await response
          .json()
          .catch(() => null);

      if (!response.ok) {
        setError(
          data?.message ||
            "Unable to load report."
        );

        return;
      }

      setReport(data);

      window.scrollTo({
        top: 0,
        behavior: "smooth",
      });
    } catch (error) {
      console.error(
        "Load report error:",
        error
      );

      setError(
        "Unable to load report."
      );
    } finally {
      setLoading(false);
    }
  };

  // =========================================================
  // DOWNLOAD PDF
  // =========================================================

  const downloadPDF = () => {
    if (!report) {
      return;
    }

    const doc = new jsPDF();

    const content = report.content;

    // -------------------------------------------------------
    // TITLE
    // -------------------------------------------------------

    doc.setFontSize(24);
    doc.setFont(
      "helvetica",
      "bold"
    );

    doc.text(
      "LOOP",
      20,
      20
    );

    doc.setFontSize(16);

    doc.text(
      "Voice of Customer Report",
      20,
      30
    );

    doc.setFontSize(10);
    doc.setFont(
      "helvetica",
      "normal"
    );

    doc.text(
      `Period: ${formatDisplayDate(
        report.periodStart
      )} - ${formatDisplayDate(
        report.periodEnd
      )}`,
      20,
      38
    );

    // -------------------------------------------------------
    // SUMMARY
    // -------------------------------------------------------

    doc.setFontSize(15);
    doc.setFont(
      "helvetica",
      "bold"
    );

    doc.text(
      "Executive Summary",
      20,
      53
    );

    doc.setFontSize(10);
    doc.setFont(
      "helvetica",
      "normal"
    );

    const summaryLines =
      doc.splitTextToSize(
        content.summary,
        170
      );

    doc.text(
      summaryLines,
      20,
      61
    );

    let currentY =
      61 +
      summaryLines.length * 5 +
      10;

    // -------------------------------------------------------
    // SENTIMENT
    // -------------------------------------------------------

    doc.setFontSize(15);
    doc.setFont(
      "helvetica",
      "bold"
    );

    doc.text(
      "Sentiment",
      20,
      currentY
    );

    autoTable(doc, {
      startY: currentY + 6,

      head: [
        [
          "Sentiment",
          "Count",
          "Percentage",
          "Change",
        ],
      ],

      body: [
        [
          "Positive",
          content.sentiment.positive.toString(),
          `${content.sentiment.positivePercentage}%`,
          formatShift(
            content.sentimentShift.positive
          ),
        ],

        [
          "Neutral",
          content.sentiment.neutral.toString(),
          `${content.sentiment.neutralPercentage}%`,
          formatShift(
            content.sentimentShift.neutral
          ),
        ],

        [
          "Negative",
          content.sentiment.negative.toString(),
          `${content.sentiment.negativePercentage}%`,
          formatShift(
            content.sentimentShift.negative
          ),
        ],
      ],

      theme: "grid",

      headStyles: {
        fontStyle: "bold",
      },
    });

    currentY =
      ((doc as any).lastAutoTable
        ?.finalY || currentY) + 15;

    // -------------------------------------------------------
    // TOP THEMES
    // -------------------------------------------------------

    if (currentY > 245) {
      doc.addPage();
      currentY = 20;
    }

    doc.setFontSize(15);
    doc.setFont(
      "helvetica",
      "bold"
    );

    doc.text(
      "Top Themes",
      20,
      currentY
    );

    autoTable(doc, {
      startY: currentY + 6,

      head: [
        [
          "Theme",
          "Feedback",
          "Percentage",
        ],
      ],

      body:
        content.topThemes.map(
          (theme) => [
            theme.name,
            theme.count.toString(),
            `${theme.percentage}%`,
          ]
        ),

      theme: "grid",

      headStyles: {
        fontStyle: "bold",
      },
    });

    currentY =
      ((doc as any).lastAutoTable
        ?.finalY || currentY) + 15;

    // -------------------------------------------------------
    // CUSTOMER VOICE
    // -------------------------------------------------------

    if (currentY > 230) {
      doc.addPage();
      currentY = 20;
    }

    doc.setFontSize(15);
    doc.setFont(
      "helvetica",
      "bold"
    );

    doc.text(
      "Customer Voice",
      20,
      currentY
    );

    autoTable(doc, {
      startY: currentY + 6,

      head: [
        [
          "Quote",
          "Sentiment",
          "Channel",
          "Date",
        ],
      ],

      body:
        content.quotes.map(
          (quote) => [
            quote.content,
            quote.sentiment,
            quote.channel,
            quote.date,
          ]
        ),

      theme: "grid",

      styles: {
        fontSize: 8,
        cellPadding: 3,
        overflow: "linebreak",
      },

      headStyles: {
        fontStyle: "bold",
      },

      columnStyles: {
        0: {
          cellWidth: 105,
        },

        1: {
          cellWidth: 25,
        },

        2: {
          cellWidth: 25,
        },

        3: {
          cellWidth: 25,
        },
      },
    });

    currentY =
      ((doc as any).lastAutoTable
        ?.finalY || currentY) + 15;

    // -------------------------------------------------------
    // RECOMMENDED ACTIONS
    // -------------------------------------------------------

    if (currentY > 230) {
      doc.addPage();
      currentY = 20;
    }

    doc.setFontSize(15);
    doc.setFont(
      "helvetica",
      "bold"
    );

    doc.text(
      "Recommended Actions",
      20,
      currentY
    );

    autoTable(doc, {
      startY: currentY + 6,

      head: [
        [
          "Recommended Action",
        ],
      ],

      body:
        content.recommendedActions.map(
          (action) => [
            action,
          ]
        ),

      theme: "grid",

      styles: {
        fontSize: 9,
        cellPadding: 4,
      },

      headStyles: {
        fontStyle: "bold",
      },
    });

    // -------------------------------------------------------
    // FOOTER
    // -------------------------------------------------------

    const pageCount =
      (doc as any)
        .internal
        .getNumberOfPages();

    for (
      let page = 1;
      page <= pageCount;
      page++
    ) {
      doc.setPage(page);

      doc.setFontSize(8);
      doc.setFont(
        "helvetica",
        "normal"
      );

      doc.text(
        `LOOP Feedback Intelligence | Page ${page} of ${pageCount}`,
        20,
        290
      );
    }

    // -------------------------------------------------------
    // SAVE
    // -------------------------------------------------------

    const fileDate =
      new Date()
        .toISOString()
        .split("T")[0];

    doc.save(
      `LOOP-Voice-of-Customer-${fileDate}.pdf`
    );
  };

  // =========================================================
  // LOADING
  // =========================================================

  if (loading && !report) {
    return (
      <div className="min-h-screen bg-gray-50 p-8">

        <h1 className="text-3xl font-bold text-gray-900">
          Reports
        </h1>

        <p className="mt-2 text-gray-500">
          Loading...
        </p>

      </div>
    );
  }

  // =========================================================
  // MAIN PAGE
  // =========================================================

  return (
    <div className="min-h-screen bg-gray-50 p-8">

      {/* HEADER */}

      <div className="mb-8 flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">

        <div>

          <h1 className="text-3xl font-bold text-gray-900">
            Reports
          </h1>

          <p className="mt-2 text-gray-500">
            Voice-of-Customer reports from your actual feedback.
          </p>

        </div>

        {report && (
          <button
            type="button"
            onClick={downloadPDF}
            className="w-fit rounded-lg bg-gray-900 px-5 py-3 text-sm font-semibold text-white hover:bg-gray-800"
          >
            Download PDF
          </button>
        )}

      </div>


      {/* ERROR */}

      {error && (
        <div className="mb-6 rounded-lg border border-red-200 bg-red-50 p-4">

          <p className="text-sm text-red-700">
            {error}
          </p>

        </div>
      )}


      {/* REPORT GENERATOR */}

      <div className="rounded-xl bg-white p-6 shadow-sm">

        <h2 className="text-lg font-semibold text-gray-900">
          Generate Voice-of-Customer Report
        </h2>

        <p className="mt-1 text-sm text-gray-500">
          Select a period and generate a report from your actual feedback.
        </p>


        {/* PERIOD BUTTONS */}

        <div className="mt-5 flex flex-wrap gap-2">

          <button
            type="button"
            onClick={() =>
              setDateRange("7")
            }
            className={`rounded-lg px-4 py-2 text-sm font-medium ${
              period === "7"
                ? "bg-gray-900 text-white"
                : "border border-gray-300 bg-white text-gray-700 hover:bg-gray-50"
            }`}
          >
            Last 7 Days
          </button>


          <button
            type="button"
            onClick={() =>
              setDateRange("30")
            }
            className={`rounded-lg px-4 py-2 text-sm font-medium ${
              period === "30"
                ? "bg-gray-900 text-white"
                : "border border-gray-300 bg-white text-gray-700 hover:bg-gray-50"
            }`}
          >
            Last 30 Days
          </button>


          <button
            type="button"
            onClick={() =>
              setDateRange("90")
            }
            className={`rounded-lg px-4 py-2 text-sm font-medium ${
              period === "90"
                ? "bg-gray-900 text-white"
                : "border border-gray-300 bg-white text-gray-700 hover:bg-gray-50"
            }`}
          >
            Last 90 Days
          </button>


          <button
            type="button"
            onClick={() =>
              setPeriod("custom")
            }
            className={`rounded-lg px-4 py-2 text-sm font-medium ${
              period === "custom"
                ? "bg-gray-900 text-white"
                : "border border-gray-300 bg-white text-gray-700 hover:bg-gray-50"
            }`}
          >
            Custom
          </button>

        </div>


        {/* DATE INPUTS */}

        <div className="mt-5 grid gap-4 md:grid-cols-2">

          <div>

            <label className="mb-2 block text-sm font-medium text-gray-700">
              Start Date
            </label>

            <input
              type="date"
              value={startDate}
              onChange={(e) => {
                setStartDate(
                  e.target.value
                );

                setPeriod(
                  "custom"
                );
              }}
              className="w-full rounded-lg border border-gray-300 px-4 py-3 text-gray-900 outline-none focus:border-gray-600"
            />

          </div>


          <div>

            <label className="mb-2 block text-sm font-medium text-gray-700">
              End Date
            </label>

            <input
              type="date"
              value={endDate}
              onChange={(e) => {
                setEndDate(
                  e.target.value
                );

                setPeriod(
                  "custom"
                );
              }}
              className="w-full rounded-lg border border-gray-300 px-4 py-3 text-gray-900 outline-none focus:border-gray-600"
            />

          </div>

        </div>


        {/* GENERATE BUTTON */}

        <div className="mt-5">

          <button
            type="button"
            onClick={generateReport}
            disabled={
              loading ||
              !startDate ||
              !endDate
            }
            className="rounded-lg bg-gray-900 px-6 py-3 font-semibold text-white hover:bg-gray-800 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {loading
              ? "Generating Report..."
              : "Generate Report"}
          </button>

        </div>

      </div>


      {/* =====================================================
          REPORT
      ====================================================== */}

      {report && (
        <div className="mt-6 space-y-6">

          {/* REPORT TITLE */}

          <div className="rounded-xl bg-white p-6 shadow-sm">

            <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">

              <div>

                <p className="text-sm font-medium text-gray-500">
                  Voice of Customer
                </p>

                <h2 className="mt-1 text-2xl font-bold text-gray-900">
                  {report.title}
                </h2>

                <p className="mt-2 text-sm text-gray-500">
                  {formatDisplayDate(
                    report.periodStart
                  )}

                  {" → "}

                  {formatDisplayDate(
                    report.periodEnd
                  )}
                </p>

              </div>


              <button
                type="button"
                onClick={downloadPDF}
                className="w-fit rounded-lg border border-gray-300 px-5 py-2.5 text-sm font-medium text-gray-700 hover:bg-gray-50"
              >
                Download PDF
              </button>

            </div>

          </div>


          {/* EXECUTIVE SUMMARY */}

          <div className="rounded-xl bg-white p-6 shadow-sm">

            <h2 className="text-lg font-semibold text-gray-900">
              Executive Summary
            </h2>

            <p className="mt-4 leading-7 text-gray-700">
              {report.content.summary}
            </p>

          </div>


          {/* STAT CARDS */}

          <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-4">

            <div className="rounded-xl bg-white p-6 shadow-sm">

              <p className="text-sm text-gray-500">
                Total Feedback
              </p>

              <p className="mt-2 text-3xl font-bold text-gray-900">
                {report.content.totalFeedback}
              </p>

            </div>


            <div className="rounded-xl bg-white p-6 shadow-sm">

              <p className="text-sm text-gray-500">
                Positive
              </p>

              <p className="mt-2 text-3xl font-bold text-green-600">
                {report.content.sentiment.positivePercentage}%
              </p>

              <p className="mt-1 text-sm text-gray-500">
                {report.content.sentiment.positive} feedback
              </p>

            </div>


            <div className="rounded-xl bg-white p-6 shadow-sm">

              <p className="text-sm text-gray-500">
                Neutral
              </p>

              <p className="mt-2 text-3xl font-bold text-gray-600">
                {report.content.sentiment.neutralPercentage}%
              </p>

              <p className="mt-1 text-sm text-gray-500">
                {report.content.sentiment.neutral} feedback
              </p>

            </div>


            <div className="rounded-xl bg-white p-6 shadow-sm">

              <p className="text-sm text-gray-500">
                Negative
              </p>

              <p className="mt-2 text-3xl font-bold text-red-600">
                {report.content.sentiment.negativePercentage}%
              </p>

              <p className="mt-1 text-sm text-gray-500">
                {report.content.sentiment.negative} feedback
              </p>

            </div>

          </div>


          {/* SENTIMENT SHIFT */}

          <div className="rounded-xl bg-white p-6 shadow-sm">

            <h2 className="text-lg font-semibold text-gray-900">
              Sentiment Shift
            </h2>

            <p className="mt-1 text-sm text-gray-500">
              Compared with the previous equivalent period.
            </p>


            <div className="mt-6 grid gap-4 md:grid-cols-3">

              <ShiftCard
                label="Positive"
                value={
                  report.content
                    .sentimentShift
                    .positive
                }
              />

              <ShiftCard
                label="Neutral"
                value={
                  report.content
                    .sentimentShift
                    .neutral
                }
              />

              <ShiftCard
                label="Negative"
                value={
                  report.content
                    .sentimentShift
                    .negative
                }
              />

            </div>

          </div>


          {/* TOP THEMES */}

          <div className="rounded-xl bg-white p-6 shadow-sm">

            <h2 className="text-lg font-semibold text-gray-900">
              Top Themes
            </h2>

            <p className="mt-1 text-sm text-gray-500">
              Most frequently associated themes.
            </p>


            <div className="mt-6 space-y-4">

              {report.content.topThemes.length === 0 ? (

                <p className="text-sm text-gray-500">
                  No theme data available.
                </p>

              ) : (

                report.content.topThemes.map(
                  (theme, index) => (
                    <div
                      key={`${theme.name}-${index}`}
                    >

                      <div className="mb-2 flex items-center justify-between">

                        <div className="flex items-center gap-3">

                          <span className="flex h-7 w-7 items-center justify-center rounded-full bg-gray-900 text-xs font-semibold text-white">
                            {index + 1}
                          </span>

                          <span className="font-medium text-gray-800">
                            {theme.name}
                          </span>

                        </div>


                        <span className="text-sm text-gray-500">
                          {theme.count} (
                          {theme.percentage}%)
                        </span>

                      </div>


                      <div className="h-2.5 overflow-hidden rounded-full bg-gray-100">

                        <div
                          className="h-full rounded-full bg-gray-800"
                          style={{
                            width:
                              `${Math.min(
                                theme.percentage,
                                100
                              )}%`,
                          }}
                        />

                      </div>

                    </div>
                  )
                )

              )}

            </div>

          </div>


          {/* CUSTOMER VOICE */}

          <div className="rounded-xl bg-white p-6 shadow-sm">

            <h2 className="text-lg font-semibold text-gray-900">
              Customer Voice
            </h2>

            <p className="mt-1 text-sm text-gray-500">
              Representative customer feedback from this period.
            </p>


            <div className="mt-6 space-y-4">

              {report.content.quotes.length === 0 ? (

                <p className="text-sm text-gray-500">
                  No customer quotes available.
                </p>

              ) : (

                report.content.quotes.map(
                  (quote, index) => {

                    const sentiment =
                      quote.sentiment
                        .toLowerCase();

                    let badge =
                      "bg-gray-100 text-gray-700";

                    if (
                      sentiment ===
                      "positive"
                    ) {
                      badge =
                        "bg-green-100 text-green-700";
                    }

                    if (
                      sentiment ===
                      "negative"
                    ) {
                      badge =
                        "bg-red-100 text-red-700";
                    }


                    return (
                      <div
                        key={`${quote.date}-${index}`}
                        className="rounded-lg border border-gray-200 p-5"
                      >

                        <p className="leading-7 text-gray-700">
                          “{quote.content}”
                        </p>


                        <div className="mt-4 flex flex-wrap items-center gap-2">

                          <span
                            className={`rounded-full px-3 py-1 text-xs font-medium ${badge}`}
                          >
                            {quote.sentiment}
                          </span>

                          <span className="text-xs text-gray-500">
                            {quote.channel}
                          </span>

                          <span className="text-xs text-gray-500">
                            {quote.date}
                          </span>

                        </div>

                      </div>
                    );
                  }
                )

              )}

            </div>

          </div>


          {/* RECOMMENDED ACTIONS */}

          <div className="rounded-xl bg-white p-6 shadow-sm">

            <h2 className="text-lg font-semibold text-gray-900">
              Recommended Actions
            </h2>

            <p className="mt-1 text-sm text-gray-500">
              Actions based on actual feedback and themes.
            </p>


            <div className="mt-6 space-y-3">

              {report.content.recommendedActions.length === 0 ? (

                <p className="text-sm text-gray-500">
                  No recommended actions available.
                </p>

              ) : (

                report.content.recommendedActions.map(
                  (action, index) => (

                    <div
                      key={index}
                      className="flex gap-4 rounded-lg border border-gray-200 p-4"
                    >

                      <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-gray-900 text-xs font-semibold text-white">
                        {index + 1}
                      </span>

                      <p className="text-sm leading-6 text-gray-700">
                        {action}
                      </p>

                    </div>

                  )
                )

              )}

            </div>

          </div>


          {/* CHANNELS */}

          <div className="rounded-xl bg-white p-6 shadow-sm">

            <h2 className="text-lg font-semibold text-gray-900">
              Feedback Channels
            </h2>


            <div className="mt-5 grid gap-3 md:grid-cols-2">

              {report.content.channels.length === 0 ? (

                <p className="text-sm text-gray-500">
                  No channel data available.
                </p>

              ) : (

                report.content.channels.map(
                  (channel) => (

                    <div
                      key={channel.name}
                      className="flex items-center justify-between rounded-lg bg-gray-50 p-4"
                    >

                      <span className="font-medium text-gray-700">
                        {channel.name}
                      </span>

                      <span className="text-sm text-gray-500">
                        {channel.count} (
                        {channel.percentage}%)
                      </span>

                    </div>

                  )
                )

              )}

            </div>

          </div>

        </div>
      )}


      {/* =====================================================
          SAVED REPORTS
      ====================================================== */}

      <div className="mt-6 rounded-xl bg-white p-6 shadow-sm">

        <div className="flex items-center justify-between">

          <div>

            <h2 className="text-lg font-semibold text-gray-900">
              Saved Reports
            </h2>

            <p className="mt-1 text-sm text-gray-500">
              Previously generated reports.
            </p>

          </div>


          <button
            type="button"
            onClick={() =>
              setShowSaved(
                !showSaved
              )
            }
            className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
          >
            {showSaved
              ? "Hide"
              : "View Reports"}
          </button>

        </div>


        {showSaved && (
          <div className="mt-5">

            {loadingSaved ? (

              <p className="text-sm text-gray-500">
                Loading saved reports...
              </p>

            ) : savedReports.length === 0 ? (

              <p className="text-sm text-gray-500">
                No saved reports yet.
              </p>

            ) : (

              <div className="space-y-3">

                {savedReports.map(
                  (saved) => (

                    <div
                      key={saved.id}
                      className="flex flex-col gap-3 rounded-lg border border-gray-200 p-4 md:flex-row md:items-center md:justify-between"
                    >

                      <div>

                        <p className="font-medium text-gray-800">
                          {saved.title}
                        </p>

                        <p className="mt-1 text-xs text-gray-500">
                          {formatDisplayDate(
                            saved.periodStart
                          )}

                          {" → "}

                          {formatDisplayDate(
                            saved.periodEnd
                          )}
                        </p>

                      </div>


                      <button
                        type="button"
                        onClick={() =>
                          viewSavedReport(
                            saved.id
                          )
                        }
                        className="w-fit rounded-lg bg-gray-900 px-4 py-2 text-sm font-medium text-white hover:bg-gray-800"
                      >
                        View Report
                      </button>

                    </div>

                  )
                )}

              </div>

            )}

          </div>
        )}

      </div>


      {/* FOOTER */}

      <div className="pb-8 pt-8 text-center">

        <p className="text-xs text-gray-400">
          LOOP Feedback Intelligence
        </p>

      </div>

    </div>
  );
}


// =============================================================
// SHIFT CARD
// =============================================================

function ShiftCard({
  label,
  value,
}: {
  label: string;
  value: number;
}) {
  const positive =
    value > 0;

  const negative =
    value < 0;

  return (
    <div className="rounded-lg border border-gray-200 p-5">

      <p className="text-sm text-gray-500">
        {label}
      </p>

      <p
        className={`mt-2 text-2xl font-bold ${
          positive
            ? "text-green-600"
            : negative
              ? "text-red-600"
              : "text-gray-600"
        }`}
      >
        {value > 0
          ? "+"
          : ""}
        {value}%
      </p>

      <p className="mt-1 text-xs text-gray-500">
        percentage-point change
      </p>

    </div>
  );
}


// =============================================================
// PDF SHIFT FORMAT
// =============================================================

function formatShift(
  value: number
) {
  if (value > 0) {
    return `+${value} pp`;
  }

  return `${value} pp`;
}