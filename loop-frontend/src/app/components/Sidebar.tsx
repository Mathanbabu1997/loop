"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";

export default function Sidebar() {
  const router = useRouter();
  const [role, setRole] = useState("");

  useEffect(() => {
    const token = localStorage.getItem("token");

    if (!token) {
      router.replace("/login");
      return;
    }

    try {
      const payload = JSON.parse(atob(token.split(".")[1]));

      const userRole =
        payload.role ||
        payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ||
        "";

      setRole(userRole);
    } catch (error) {
      console.error("Unable to read user role:", error);
    }
  }, [router]);

  const handleLogout = () => {
    localStorage.removeItem("token");
    router.replace("/login");
  };

  return (
    <aside className="fixed left-0 top-0 z-50 h-screen w-64 bg-gray-900 text-white">

      {/* Logo */}
      <div className="border-b border-gray-700 px-6 py-5">
        <h1 className="text-2xl font-bold">
          LOOP
        </h1>

        <p className="mt-1 text-xs text-gray-400">
          Feedback Intelligence
        </p>
      </div>

      {/* Navigation */}
      <nav className="space-y-2 p-4">

        <Link
          href="/dashboard"
          className="block rounded-lg px-4 py-3 text-gray-300 hover:bg-gray-800 hover:text-white"
        >
          Dashboard
        </Link>

        <Link
          href="/inbox"
          className="block rounded-lg px-4 py-3 text-gray-300 hover:bg-gray-800 hover:text-white"
        >
          Inbox
        </Link>

        <Link
          href="/trends"
          className="block rounded-lg px-4 py-3 text-gray-300 hover:bg-gray-800 hover:text-white"
        >
          Trends
        </Link>

        <Link
          href="/ask"
          className="block rounded-lg px-4 py-3 text-gray-300 hover:bg-gray-800 hover:text-white"
        >
          Ask LOOP
        </Link>

        <Link
          href="/reports"
          className="block rounded-lg px-4 py-3 text-gray-300 hover:bg-gray-800 hover:text-white"
        >
          Reports
        </Link>

        <Link
          href="/settings"
          className="block rounded-lg px-4 py-3 text-gray-300 hover:bg-gray-800 hover:text-white"
        >
          Settings
        </Link>

        {/* ADMIN ONLY */}
        {role === "ADMIN" && (
          <>
            <div className="mt-6 border-t border-gray-700 pt-4">
              <p className="px-4 pb-2 text-xs font-semibold uppercase tracking-wider text-gray-500">
                Administration
              </p>

              <Link
                href="/members"
                className="block rounded-lg px-4 py-3 text-gray-300 hover:bg-gray-800 hover:text-white"
              >
                Members
              </Link>
            </div>
          </>
        )}

      </nav>

      {/* Bottom */}
      <div className="absolute bottom-0 left-0 right-0 border-t border-gray-700 p-4">

        <p className="mb-3 text-sm text-gray-400">
          LOOP Platform
        </p>

        <button
          onClick={handleLogout}
          className="w-full rounded-lg px-4 py-3 text-left text-red-400 hover:bg-gray-800 hover:text-red-300"
        >
          Logout
        </button>

      </div>

    </aside>
  );
}