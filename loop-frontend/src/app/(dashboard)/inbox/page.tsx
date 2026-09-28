"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";

type Feedback = {
  id: string;
  content: string;
  channel: string;
  customerLabel: string;
  sentiment: string;
  sentimentScore: number | null;
  status: string;
  createdAt: string;
};

type FeedbackResponse = {
  items: Feedback[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};

export default function InboxPage() {
  const router = useRouter();

  // =====================================================
  // Feedback
  // =====================================================

  const [feedback, setFeedback] = useState<Feedback[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  // =====================================================
  // Filters
  // =====================================================

  const [search, setSearch] = useState("");
  const [sentiment, setSentiment] = useState("");
  const [status, setStatus] = useState("");
  const [channel, setChannel] = useState("");

  // =====================================================
  // Pagination
  // =====================================================

  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalItems, setTotalItems] = useState(0);

  const pageSize = 10;

  // =====================================================
  // Role
  // =====================================================

  const [role, setRole] = useState("");

  // =====================================================
  // CSV Upload
  // =====================================================

  const [selectedFile, setSelectedFile] =
    useState<File | null>(null);

  const [uploading, setUploading] = useState(false);

  const [uploadMessage, setUploadMessage] =
    useState("");

  const [uploadError, setUploadError] =
    useState("");

  // =====================================================
  // Load User Role
  // =====================================================

  useEffect(() => {
    const token = localStorage.getItem("token");

    if (!token) {
      router.replace("/login");
      return;
    }

    try {
      const payload = JSON.parse(
        atob(token.split(".")[1])
      );

      const userRole =
        payload.role ||
        payload[
          "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        ] ||
        "";

      setRole(userRole);
    } catch (error) {
      console.error(
        "Unable to read user role:",
        error
      );
    }
  }, [router]);

  // =====================================================
  // Load Feedback
  // =====================================================

  const loadFeedback = async () => {
    const token = localStorage.getItem("token");

    if (!token) {
      router.replace("/login");
      return;
    }

    setLoading(true);
    setError("");

    try {
      const params = new URLSearchParams();

      params.append("page", page.toString());
      params.append("pageSize", pageSize.toString());

      if (search.trim()) {
        params.append("search", search.trim());
      }

      if (sentiment) {
        params.append("sentiment", sentiment);
      }

      if (status) {
        params.append("status", status);
      }

      if (channel) {
        params.append("channel", channel);
      }

      const response = await fetch(
        `https://loop-api-s464.onrender.com/api/Feedback?${params.toString()}`,
        {
          method: "GET",
          headers: {
            Authorization: `Bearer ${token}`,
            "Content-Type": "application/json",
          },
        }
      );

      if (response.status === 401) {
        localStorage.removeItem("token");
        router.replace("/login");
        return;
      }

      if (!response.ok) {
        throw new Error(
          `API Error: ${response.status}`
        );
      }

      const result =
        (await response.json()) as FeedbackResponse;

      setFeedback(result.items);
      setTotalPages(result.totalPages);
      setTotalItems(result.totalItems);
    } catch (err: unknown) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("Failed to load feedback.");
      }
    } finally {
      setLoading(false);
    }
  };

  // =====================================================
  // Load feedback when filters/page change
  // =====================================================

  useEffect(() => {
    loadFeedback();
  }, [
    page,
    sentiment,
    status,
    channel,
  ]);

  // =====================================================
  // Search
  // =====================================================

  const handleSearch = () => {
    setPage(1);
    loadFeedback();
  };

  // =====================================================
  // Clear Filters
  // =====================================================

  const clearFilters = () => {
    setSearch("");
    setSentiment("");
    setStatus("");
    setChannel("");
    setPage(1);
  };

  // =====================================================
  // Sentiment Style
  // =====================================================

  const getSentimentStyle = (value: string) => {
    switch (value.toUpperCase()) {
      case "POS":
      case "POSITIVE":
        return "bg-green-100 text-green-700";

      case "NEG":
      case "NEGATIVE":
        return "bg-red-100 text-red-700";

      case "NEU":
      case "NEUTRAL":
        return "bg-yellow-100 text-yellow-700";

      default:
        return "bg-gray-100 text-gray-700";
    }
  };

  // =====================================================
  // Status Style
  // =====================================================

  const getStatusStyle = (value: string) => {
    switch (value.toUpperCase()) {
      case "NEW":
        return "bg-blue-100 text-blue-700";

      case "REVIEWED":
        return "bg-purple-100 text-purple-700";

      case "RESOLVED":
        return "bg-green-100 text-green-700";

      default:
        return "bg-gray-100 text-gray-700";
    }
  };

  // =====================================================
  // CSV File Selection
  // =====================================================

  const handleFileChange = (
    e: React.ChangeEvent<HTMLInputElement>
  ) => {
    const file = e.target.files?.[0] || null;

    setSelectedFile(file);

    setUploadMessage("");
    setUploadError("");

    if (file && !file.name.toLowerCase().endsWith(".csv")) {
      setUploadError(
        "Only CSV files are allowed."
      );
      setSelectedFile(null);
    }
  };

  // =====================================================
  // CSV Upload
  // =====================================================

  const handleCsvUpload = async () => {
    if (!selectedFile) {
      setUploadError(
        "Please select a CSV file."
      );
      return;
    }

    if (
      !selectedFile.name
        .toLowerCase()
        .endsWith(".csv")
    ) {
      setUploadError(
        "Only CSV files are allowed."
      );
      return;
    }

    try {
      setUploading(true);
      setUploadMessage("");
      setUploadError("");

      const token = localStorage.getItem("token");

      if (!token) {
        router.replace("/login");
        return;
      }

      const formData = new FormData();

      formData.append(
        "file",
        selectedFile
      );

      const response = await fetch(
        "https://loop-api-s464.onrender.com/api/Feedback/import",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${token}`,
          },
          body: formData,
        }
      );

      // Unauthorized
      if (response.status === 401) {
        localStorage.removeItem("token");
        router.replace("/login");
        return;
      }

      // Forbidden
      if (response.status === 403) {
        setUploadError(
          "You do not have permission to upload CSV files."
        );
        return;
      }

      const result = await response.json();

      if (!response.ok) {
        setUploadError(
          result.message ||
            "CSV upload failed."
        );
        return;
      }

      // Success
      setUploadMessage(
        `CSV upload completed. ${result.successful} imported, ${result.failed} failed.`
      );

      // Clear selected file
      setSelectedFile(null);

      const fileInput =
        document.getElementById(
          "csvFile"
        ) as HTMLInputElement | null;

      if (fileInput) {
        fileInput.value = "";
      }

      // Refresh feedback
      await loadFeedback();

    } catch (error) {
      console.error(
        "CSV upload error:",
        error
      );

      setUploadError(
        "Unable to upload CSV file. Please try again."
      );
    } finally {
      setUploading(false);
    }
  };

  // =====================================================
  // Page
  // =====================================================

  return (
    <main className="min-h-screen bg-gray-100 p-6 md:p-8">

      {/* =================================================
          Header
          ================================================= */}

      <div className="mb-6">

        <h1 className="text-3xl font-bold text-gray-900">
          Feedback Inbox
        </h1>

        <p className="mt-1 text-gray-500">
          Review and analyze customer feedback
        </p>

      </div>


      {/* =================================================
          CSV Upload
          ADMIN + ANALYST ONLY
          ================================================= */}

      {(role === "ADMIN" ||
        role === "ANALYST") && (

        <div className="mb-6 rounded-2xl bg-white p-5 shadow-sm">

          <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">

            <div>

              <h2 className="text-lg font-semibold text-gray-900">
                Import Feedback
              </h2>

              <p className="mt-1 text-sm text-gray-500">
                Upload a CSV file to import multiple
                feedback records.
              </p>

            </div>


            <div className="flex flex-col gap-3 sm:flex-row">

              <input
                id="csvFile"
                type="file"
                accept=".csv"
                onChange={handleFileChange}
                className="block w-full rounded-lg border border-gray-300 bg-white text-sm text-gray-700 file:mr-4 file:border-0 file:bg-gray-100 file:px-4 file:py-2.5 file:text-sm file:font-medium file:text-gray-700 hover:file:bg-gray-200"
              />

              <button
                type="button"
                onClick={handleCsvUpload}
                disabled={
                  !selectedFile ||
                  uploading
                }
                className="whitespace-nowrap rounded-lg bg-gray-900 px-5 py-2.5 text-sm font-medium text-white hover:bg-gray-700 disabled:cursor-not-allowed disabled:opacity-50"
              >
                {uploading
                  ? "Uploading..."
                  : "Upload CSV"}
              </button>

            </div>

          </div>


          {/* Upload Success */}

          {uploadMessage && (

            <div className="mt-4 rounded-lg border border-green-200 bg-green-50 px-4 py-3">

              <p className="text-sm font-medium text-green-700">
                {uploadMessage}
              </p>

            </div>

          )}


          {/* Upload Error */}

          {uploadError && (

            <div className="mt-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3">

              <p className="text-sm font-medium text-red-700">
                {uploadError}
              </p>

            </div>

          )}

        </div>

      )}


      {/* =================================================
          Filters
          ================================================= */}

      <div className="rounded-2xl bg-white p-5 shadow-sm">

        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">

          {/* Search */}

          <div className="lg:col-span-2">

            <label className="mb-2 block text-sm font-medium text-gray-700">
              Search
            </label>

            <div className="flex gap-2">

              <input
                type="text"
                value={search}
                onChange={(e) =>
                  setSearch(e.target.value)
                }
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    handleSearch();
                  }
                }}
                placeholder="Search feedback..."
                className="w-full rounded-lg border border-gray-300 px-4 py-2.5 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
              />

              <button
                onClick={handleSearch}
                className="rounded-lg bg-blue-600 px-5 py-2.5 font-medium text-white hover:bg-blue-700"
              >
                Search
              </button>

            </div>

          </div>


          {/* Sentiment */}

          <div>

            <label className="mb-2 block text-sm font-medium text-gray-700">
              Sentiment
            </label>

            <select
              value={sentiment}
              onChange={(e) => {
                setSentiment(
                  e.target.value
                );
                setPage(1);
              }}
              className="w-full rounded-lg border border-gray-300 bg-white px-4 py-2.5 outline-none focus:border-blue-500"
            >

              <option value="">
                All Sentiments
              </option>

              <option value="POS">
                Positive
              </option>

              <option value="NEG">
                Negative
              </option>

              <option value="NEU">
                Neutral
              </option>

            </select>

          </div>


          {/* Status */}

          <div>

            <label className="mb-2 block text-sm font-medium text-gray-700">
              Status
            </label>

            <select
              value={status}
              onChange={(e) => {
                setStatus(
                  e.target.value
                );
                setPage(1);
              }}
              className="w-full rounded-lg border border-gray-300 bg-white px-4 py-2.5 outline-none focus:border-blue-500"
            >

              <option value="">
                All Status
              </option>

              <option value="NEW">
                New
              </option>

              <option value="REVIEWED">
                Reviewed
              </option>

              <option value="RESOLVED">
                Resolved
              </option>

            </select>

          </div>

        </div>


        {/* Second Filter Row */}

        <div className="mt-4 flex flex-wrap items-end gap-4">

          {/* Channel */}

          <div className="w-full md:w-64">

            <label className="mb-2 block text-sm font-medium text-gray-700">
              Channel
            </label>

            <select
              value={channel}
              onChange={(e) => {
                setChannel(
                  e.target.value
                );
                setPage(1);
              }}
              className="w-full rounded-lg border border-gray-300 bg-white px-4 py-2.5 outline-none focus:border-blue-500"
            >

              <option value="">
                All Channels
              </option>

              <option value="EMAIL">
                Email
              </option>

              <option value="WEB">
                Web
              </option>

              <option value="APP">
                App
              </option>

              <option value="SOCIAL">
                Social
              </option>

            </select>

          </div>


          {/* Clear Filters */}

          <button
            onClick={clearFilters}
            className="rounded-lg border border-gray-300 bg-white px-5 py-2.5 font-medium text-gray-700 hover:bg-gray-50"
          >
            Clear Filters
          </button>

        </div>

      </div>


      {/* =================================================
          Result Count
          ================================================= */}

      <div className="mt-6 flex items-center justify-between">

        <div>

          <p className="text-sm text-gray-500">

            Showing{" "}

            <span className="font-semibold text-gray-700">
              {feedback.length}
            </span>{" "}

            of{" "}

            <span className="font-semibold text-gray-700">
              {totalItems}
            </span>{" "}

            feedback records

          </p>

        </div>

      </div>


      {/* =================================================
          Error
          ================================================= */}

      {error && (

        <div className="mt-4 rounded-xl border border-red-200 bg-red-50 p-4">

          <p className="text-sm text-red-600">
            {error}
          </p>

        </div>

      )}


      {/* =================================================
          Loading
          ================================================= */}

      {loading ? (

        <div className="mt-6 rounded-2xl bg-white p-12 text-center shadow-sm">

          <div className="mx-auto mb-4 h-10 w-10 animate-spin rounded-full border-4 border-gray-200 border-t-blue-600" />

          <p className="text-gray-500">
            Loading feedback...
          </p>

        </div>

      ) : feedback.length === 0 ? (

        /* =================================================
           Empty
           ================================================= */

        <div className="mt-6 rounded-2xl bg-white p-12 text-center shadow-sm">

          <h2 className="text-lg font-semibold text-gray-800">
            No feedback found
          </h2>

          <p className="mt-2 text-sm text-gray-500">
            Try changing your search or filters.
          </p>

        </div>

      ) : (

        /* =================================================
           Table
           ================================================= */

        <div className="mt-6 overflow-hidden rounded-2xl bg-white shadow-sm">

          <div className="overflow-x-auto">

            <table className="w-full min-w-[900px]">

              <thead className="border-b bg-gray-50">

                <tr>

                  <th className="px-6 py-4 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">
                    Customer
                  </th>

                  <th className="px-6 py-4 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">
                    Feedback
                  </th>

                  <th className="px-6 py-4 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">
                    Channel
                  </th>

                  <th className="px-6 py-4 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">
                    Sentiment
                  </th>

                  <th className="px-6 py-4 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">
                    Status
                  </th>

                  <th className="px-6 py-4 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">
                    Date
                  </th>

                </tr>

              </thead>


              <tbody className="divide-y divide-gray-100">

                {feedback.map((item) => (

                  <tr
                    key={item.id}
                    className="hover:bg-gray-50"
                  >

                    {/* Customer */}

                    <td className="whitespace-nowrap px-6 py-4">

                      <p className="font-medium text-gray-800">
                        {item.customerLabel ||
                          "Unknown"}
                      </p>

                    </td>


                    {/* Feedback */}

                    <td className="max-w-md px-6 py-4">

                      <p
                        className="truncate text-sm text-gray-700"
                        title={item.content}
                      >
                        {item.content}
                      </p>

                    </td>


                    {/* Channel */}

                    <td className="whitespace-nowrap px-6 py-4">

                      <span className="text-sm text-gray-600">
                        {item.channel}
                      </span>

                    </td>


                    {/* Sentiment */}

                    <td className="whitespace-nowrap px-6 py-4">

                      <span
                        className={`rounded-full px-3 py-1 text-xs font-semibold ${getSentimentStyle(
                          item.sentiment
                        )}`}
                      >
                        {item.sentiment}
                      </span>

                    </td>


                    {/* Status */}

                    <td className="whitespace-nowrap px-6 py-4">

                      <span
                        className={`rounded-full px-3 py-1 text-xs font-semibold ${getStatusStyle(
                          item.status
                        )}`}
                      >
                        {item.status}
                      </span>

                    </td>


                    {/* Date */}

                    <td className="whitespace-nowrap px-6 py-4 text-sm text-gray-500">

                      {new Date(
                        item.createdAt
                      ).toLocaleDateString(
                        "en-IN",
                        {
                          day: "2-digit",
                          month: "short",
                          year: "numeric",
                        }
                      )}

                    </td>

                  </tr>

                ))}

              </tbody>

            </table>

          </div>

        </div>

      )}


      {/* =================================================
          Pagination
          ================================================= */}

      {!loading &&
        feedback.length > 0 && (

          <div className="mt-6 flex items-center justify-between rounded-2xl bg-white p-4 shadow-sm">

            <p className="text-sm text-gray-500">

              Page{" "}

              <span className="font-semibold text-gray-700">
                {page}
              </span>{" "}

              of{" "}

              <span className="font-semibold text-gray-700">
                {totalPages}
              </span>

            </p>


            <div className="flex gap-2">

              <button
                disabled={page <= 1}
                onClick={() =>
                  setPage(
                    (p) => p - 1
                  )
                }
                className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-40"
              >
                Previous
              </button>


              <button
                disabled={
                  page >= totalPages
                }
                onClick={() =>
                  setPage(
                    (p) => p + 1
                  )
                }
                className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-40"
              >
                Next
              </button>

            </div>

          </div>

        )}

    </main>
  );
}