"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
} from "recharts";

type SentimentItem = {
  sentiment: string;
  count: number;
};

type VolumeItem = {
  date: string;
  count: number;
};

type ThemeItem = {
  themeId: string;
  themeName: string;
  count: number;
};

type DashboardResponse = {
  totalFeedback: number;
  negativePercentage: number;
  newThisWeek: number;
  sentimentBreakdown: SentimentItem[];
  volumeOverTime: VolumeItem[];
  topThemes: ThemeItem[];
};

export default function DashboardPage() {
  const router = useRouter();

  const [data, setData] = useState<DashboardResponse | null>(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token = localStorage.getItem("token");

    // No token → go to login
    if (!token) {
      router.replace("/login");
      return;
    }

    const loadDashboard = async () => {
      try {
        const response = await fetch(
          "https://localhost:7248/api/Dashboard",
          {
            method: "GET",
            headers: {
              Authorization: `Bearer ${token}`,
              "Content-Type": "application/json",
            },
          }
        );

        // Token may be invalid/expired
        if (response.status === 401) {
          localStorage.removeItem("token");
          router.replace("/login");
          return;
        }

        if (!response.ok) {
          throw new Error(`API Error: ${response.status}`);
        }

        const result =
          (await response.json()) as DashboardResponse;

        setData(result);
      } catch (err: unknown) {
        if (err instanceof Error) {
          setError(err.message);
        } else {
          setError("Failed to load dashboard.");
        }
      } finally {
        setLoading(false);
      }
    };

    loadDashboard();
  }, [router]);

  // Loading
  if (loading) {
    return (
      <main className="min-h-screen bg-gray-100 p-8">
        <div className="flex min-h-[400px] items-center justify-center">
          <div className="text-center">
            <div className="mx-auto mb-4 h-10 w-10 animate-spin rounded-full border-4 border-gray-200 border-t-blue-600" />

            <p className="text-gray-500">
              Loading dashboard...
            </p>
          </div>
        </div>
      </main>
    );
  }

  // Error
  if (error) {
    return (
      <main className="min-h-screen bg-gray-100 p-6 md:p-8">
        <div className="rounded-2xl border border-red-200 bg-red-50 p-6">
          <h2 className="text-lg font-semibold text-red-700">
            Unable to load dashboard
          </h2>

          <p className="mt-2 text-sm text-red-600">
            {error}
          </p>

          <button
            onClick={() => window.location.reload()}
            className="mt-4 rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700"
          >
            Try Again
          </button>
        </div>
      </main>
    );
  }

  if (!data) {
    return null;
  }

  // Feedback volume chart
  const chartData = data.volumeOverTime.map((item) => ({
    date: new Date(item.date).toLocaleDateString("en-IN", {
      day: "2-digit",
      month: "short",
    }),
    count: item.count,
  }));

  // Sentiment chart
  const sentimentData = data.sentimentBreakdown.map((item) => ({
    name: item.sentiment,
    value: item.count,
  }));

  return (
    <main className="min-h-screen bg-gray-100 p-6 md:p-8">

      {/* ================= HEADER ================= */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">
          LOOP Dashboard
        </h1>

        <p className="mt-1 text-gray-500">
          Customer feedback intelligence overview
        </p>
      </div>

      {/* ================= SUMMARY CARDS ================= */}
      <div className="grid gap-5 md:grid-cols-3">

        {/* Total Feedback */}
        <div className="rounded-2xl bg-white p-6 shadow-sm">
          <p className="text-sm font-medium text-gray-500">
            Total Feedback
          </p>

          <p className="mt-3 text-3xl font-bold text-gray-900">
            {data.totalFeedback}
          </p>

          <p className="mt-2 text-sm text-gray-400">
            All feedback received
          </p>
        </div>

        {/* Negative Feedback */}
        <div className="rounded-2xl bg-white p-6 shadow-sm">
          <p className="text-sm font-medium text-gray-500">
            Negative Feedback
          </p>

          <p className="mt-3 text-3xl font-bold text-red-600">
            {data.negativePercentage}%
          </p>

          <p className="mt-2 text-sm text-gray-400">
            Of total feedback
          </p>
        </div>

        {/* New This Week */}
        <div className="rounded-2xl bg-white p-6 shadow-sm">
          <p className="text-sm font-medium text-gray-500">
            New This Week
          </p>

          <p className="mt-3 text-3xl font-bold text-blue-600">
            {data.newThisWeek}
          </p>

          <p className="mt-2 text-sm text-gray-400">
            Recently received
          </p>
        </div>

      </div>

      {/* ================= CHARTS ================= */}
      <div className="mt-6 grid gap-6 lg:grid-cols-3">

        {/* Feedback Volume */}
        <div className="rounded-2xl bg-white p-6 shadow-sm lg:col-span-2">

          <div className="mb-5">
            <h2 className="text-lg font-semibold text-gray-900">
              Feedback Volume
            </h2>

            <p className="text-sm text-gray-500">
              Feedback received over time
            </p>
          </div>

          <div className="h-80">

            <ResponsiveContainer
              width="100%"
              height="100%"
            >
              <LineChart data={chartData}>

                <CartesianGrid
                  strokeDasharray="3 3"
                />

                <XAxis
                  dataKey="date"
                />

                <YAxis
                  allowDecimals={false}
                />

                <Tooltip />

                <Line
                  type="monotone"
                  dataKey="count"
                  stroke="#2563eb"
                  strokeWidth={3}
                  dot={{ r: 4 }}
                />

              </LineChart>
            </ResponsiveContainer>

          </div>

        </div>

        {/* Sentiment */}
        <div className="rounded-2xl bg-white p-6 shadow-sm">

          <div className="mb-5">
            <h2 className="text-lg font-semibold text-gray-900">
              Sentiment
            </h2>

            <p className="text-sm text-gray-500">
              Feedback sentiment distribution
            </p>
          </div>

          <div className="h-64">

            <ResponsiveContainer
              width="100%"
              height="100%"
            >
              <PieChart>

                <Pie
                  data={sentimentData}
                  dataKey="value"
                  nameKey="name"
                  cx="50%"
                  cy="50%"
                  outerRadius={90}
                  label
                >

                  {sentimentData.map((entry) => (
                    <Cell
                      key={entry.name}
                      fill={
                        entry.name === "NEG"
                          ? "#ef4444"
                          : entry.name === "POS"
                          ? "#22c55e"
                          : "#f59e0b"
                      }
                    />
                  ))}

                </Pie>

                <Tooltip />

              </PieChart>
            </ResponsiveContainer>

          </div>

          {/* Legend */}
          <div className="flex flex-wrap justify-center gap-5 text-sm">

            <span className="text-green-600">
              ● Positive
            </span>

            <span className="text-red-600">
              ● Negative
            </span>

            <span className="text-yellow-600">
              ● Neutral
            </span>

          </div>

        </div>

      </div>

      {/* ================= TOP THEMES ================= */}
      <div className="mt-6 rounded-2xl bg-white p-6 shadow-sm">

        <div className="mb-5">
          <h2 className="text-lg font-semibold text-gray-900">
            Top Themes
          </h2>

          <p className="text-sm text-gray-500">
            Most common feedback themes
          </p>
        </div>

        {data.topThemes.length === 0 ? (

          <div className="rounded-lg bg-gray-50 p-6 text-center">
            <p className="text-sm text-gray-500">
              No feedback themes available yet.
            </p>
          </div>

        ) : (

          <div className="space-y-4">

            {data.topThemes.map((theme) => (

              <div
                key={theme.themeId}
                className="flex items-center justify-between rounded-lg bg-gray-50 px-4 py-3"
              >

                <span className="font-medium text-gray-700">
                  {theme.themeName}
                </span>

                <span className="rounded-full bg-gray-200 px-3 py-1 text-sm font-semibold text-gray-700">
                  {theme.count}
                </span>

              </div>

            ))}

          </div>

        )}

      </div>

    </main>
  );
}