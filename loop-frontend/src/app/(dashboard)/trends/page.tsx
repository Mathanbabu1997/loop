"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";

type VolumeItem = {
  date: string;
  count: number;
};

type SentimentItem = {
  sentiment: string;
  count: number;
};

type ThemeItem = {
  themeId: string;
  themeName: string;
  count: number;
};

type DashboardData = {
  volumeOverTime: VolumeItem[];
  sentimentBreakdown: SentimentItem[];
  topThemes: ThemeItem[];
};

export default function TrendsPage() {
  const router = useRouter();

  const [data, setData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const loadTrends = async () => {
      const token = localStorage.getItem("token");

      if (!token) {
        router.replace("/login");
        return;
      }

      try {
        const response = await fetch(
          "https://loop-api-s464.onrender.com/api/Dashboard",
          {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          }
        );

        if (response.status === 401) {
          localStorage.removeItem("token");
          router.replace("/login");
          return;
        }

        if (!response.ok) {
          throw new Error("Failed to load trends.");
        }

        const result = await response.json();

        setData(result);
      } catch (err) {
        if (err instanceof Error) {
          setError(err.message);
        } else {
          setError("Failed to load trends.");
        }
      } finally {
        setLoading(false);
      }
    };

    loadTrends();
  }, [router]);

  if (loading) {
    return (
      <div className="p-8">
        <h1 className="text-2xl font-bold text-gray-900">
          Trends
        </h1>

        <p className="mt-4 text-gray-500">
          Loading trends...
        </p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-8">
        <h1 className="text-2xl font-bold text-gray-900">
          Trends
        </h1>

        <p className="mt-4 text-red-600">
          {error}
        </p>
      </div>
    );
  }

  if (!data) {
    return null;
  }

  return (
    <div className="p-8">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">
          Trends
        </h1>

        <p className="mt-2 text-gray-500">
          Understand how customer feedback changes over time.
        </p>
      </div>

      {/* Feedback Volume */}
      <div className="rounded-xl bg-white p-6 shadow-sm">
        <h2 className="text-xl font-semibold text-gray-900">
          Feedback Volume
        </h2>

        <div className="mt-6 space-y-3">
          {data.volumeOverTime.length === 0 ? (
            <p className="text-gray-500">
              No feedback data available.
            </p>
          ) : (
            data.volumeOverTime.map((item) => (
              <div
                key={item.date}
                className="flex items-center gap-4"
              >
                <div className="w-28 text-sm text-gray-600">
                  {new Date(item.date).toLocaleDateString()}
                </div>

                <div className="flex-1">
                  <div
                    className="h-8 rounded bg-blue-500"
                    style={{
                      width: `${Math.max(
                        item.count * 10,
                        10
                      )}%`,
                    }}
                  />
                </div>

                <div className="w-12 text-right font-semibold text-gray-900">
                  {item.count}
                </div>
              </div>
            ))
          )}
        </div>
      </div>

      {/* Sentiment */}
      <div className="mt-6 rounded-xl bg-white p-6 shadow-sm">
        <h2 className="text-xl font-semibold text-gray-900">
          Sentiment Breakdown
        </h2>

        <div className="mt-6 grid grid-cols-1 gap-4 md:grid-cols-3">
          {data.sentimentBreakdown.map((item) => (
            <div
              key={item.sentiment}
              className="rounded-lg border border-gray-200 p-5"
            >
              <p className="text-sm text-gray-500">
                {item.sentiment}
              </p>

              <p className="mt-2 text-3xl font-bold text-gray-900">
                {item.count}
              </p>
            </div>
          ))}
        </div>
      </div>

      {/* Top Themes */}
      <div className="mt-6 rounded-xl bg-white p-6 shadow-sm">
        <h2 className="text-xl font-semibold text-gray-900">
          Top Themes
        </h2>

        <div className="mt-6 space-y-4">
          {data.topThemes.length === 0 ? (
            <p className="text-gray-500">
              No themes available yet.
            </p>
          ) : (
            data.topThemes.map((theme, index) => (
              <div
                key={theme.themeId}
                className="flex items-center justify-between border-b border-gray-100 pb-3"
              >
                <div className="flex items-center gap-3">
                  <span className="font-semibold text-gray-400">
                    #{index + 1}
                  </span>

                  <span className="font-medium text-gray-900">
                    {theme.themeName}
                  </span>
                </div>

                <span className="font-semibold text-gray-700">
                  {theme.count}
                </span>
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
}