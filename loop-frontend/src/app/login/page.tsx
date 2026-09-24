"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";

export default function LoginPage() {
  const router = useRouter();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleLogin = async (
    e: FormEvent<HTMLFormElement>
  ) => {
    e.preventDefault();

    setError("");
    setLoading(true);

    try {
      const response = await fetch(
        "https://localhost:7248/api/Auth/login",
        {
          method: "POST",

          headers: {
            "Content-Type": "application/json",
          },

          body: JSON.stringify({
            email: email.trim(),
            password,
          }),
        }
      );

      // Read response safely
      const responseText = await response.text();

      console.log(
        "Login Status:",
        response.status
      );

      console.log(
        "Login Response:",
        responseText
      );

      let data: any = {};

      if (responseText) {
        try {
          data = JSON.parse(responseText);
        } catch {
          data = {
            message: responseText,
          };
        }
      }

      // Login failed
      if (!response.ok) {
        throw new Error(
          data?.message ||
            `Login failed. Server returned ${response.status}.`
        );
      }

      // Make sure JWT exists
      if (!data?.token) {
        throw new Error(
          "Login successful, but JWT token was not received."
        );
      }

      // =====================================================
      // Save JWT
      // =====================================================

      localStorage.setItem(
        "token",
        data.token
      );

      // =====================================================
      // Optional user information
      // These are NOT required for workspace security.
      // Workspace is taken from JWT by the backend.
      // =====================================================

      if (data.userId) {
        localStorage.setItem(
          "userId",
          data.userId
        );
      }

      if (data.name) {
        localStorage.setItem(
          "userName",
          data.name
        );
      }

      if (data.email) {
        localStorage.setItem(
          "userEmail",
          data.email
        );
      }

      if (data.role) {
        localStorage.setItem(
          "userRole",
          data.role
        );
      }

      // =====================================================
      // Go to dashboard
      // =====================================================

      router.push("/dashboard");

    } catch (err) {

      console.error(
        "Login Error:",
        err
      );

      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError(
          "Something went wrong while logging in."
        );
      }

    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="min-h-screen bg-gray-100 flex items-center justify-center px-4">

      <div className="w-full max-w-md">

        {/* =================================================
            LOGIN CARD
        ================================================= */}

        <div className="bg-white rounded-2xl shadow-lg p-8">

          {/* =================================================
              LOGO / TITLE
          ================================================= */}

          <div className="text-center mb-8">

            <h1 className="text-4xl font-bold text-gray-900">
              LOOP
            </h1>

            <p className="mt-2 text-gray-500">
              Customer Feedback Intelligence
            </p>

          </div>

          {/* =================================================
              LOGIN FORM
          ================================================= */}

          <form
            onSubmit={handleLogin}
            className="space-y-5"
          >

            {/* =================================================
                EMAIL
            ================================================= */}

            <div>

              <label
                htmlFor="email"
                className="block text-sm font-medium text-gray-700 mb-2"
              >
                Email
              </label>

              <input
                id="email"
                type="email"
                value={email}
                onChange={(e) =>
                  setEmail(e.target.value)
                }
                placeholder="Enter your email"
                required
                disabled={loading}
                autoComplete="email"
                className="w-full rounded-lg border border-gray-300
                           px-4 py-3
                           text-gray-900
                           outline-none
                           focus:border-blue-500
                           focus:ring-2 focus:ring-blue-200
                           disabled:bg-gray-100
                           disabled:cursor-not-allowed"
              />

            </div>

            {/* =================================================
                PASSWORD
            ================================================= */}

            <div>

              <label
                htmlFor="password"
                className="block text-sm font-medium text-gray-700 mb-2"
              >
                Password
              </label>

              <input
                id="password"
                type="password"
                value={password}
                onChange={(e) =>
                  setPassword(e.target.value)
                }
                placeholder="Enter your password"
                required
                disabled={loading}
                autoComplete="current-password"
                className="w-full rounded-lg border border-gray-300
                           px-4 py-3
                           text-gray-900
                           outline-none
                           focus:border-blue-500
                           focus:ring-2 focus:ring-blue-200
                           disabled:bg-gray-100
                           disabled:cursor-not-allowed"
              />

            </div>

            {/* =================================================
                ERROR MESSAGE
            ================================================= */}

            {error && (
              <div className="rounded-lg bg-red-50 border border-red-200 p-3">

                <p className="text-sm text-red-600">
                  {error}
                </p>

              </div>
            )}

            {/* =================================================
                SIGN IN BUTTON
            ================================================= */}

            <button
              type="submit"
              disabled={loading}
              className="w-full rounded-lg bg-blue-600
                         px-4 py-3
                         font-semibold text-white
                         hover:bg-blue-700
                         transition
                         disabled:cursor-not-allowed
                         disabled:opacity-60"
            >
              {loading
                ? "Signing in..."
                : "Sign In"}
            </button>

          </form>

          {/* =================================================
              SIGNUP LINK
          ================================================= */}

          <div className="mt-6 text-center">

            <p className="text-sm text-gray-500">

              Don't have an account?{" "}

              <button
                type="button"
                onClick={() =>
                  router.push("/signup")
                }
                disabled={loading}
                className="font-semibold text-blue-600
                           hover:text-blue-700
                           disabled:opacity-50"
              >
                Create Account
              </button>

            </p>

          </div>

          {/* =================================================
              FOOTER
          ================================================= */}

          <p className="text-center text-xs text-gray-400 mt-6">
            LOOP AI Customer Feedback Intelligence Platform
          </p>

        </div>

      </div>

    </main>
  );
}