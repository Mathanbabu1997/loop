"use client";

import {
  FormEvent,
  useEffect,
  useState,
} from "react";

import { useRouter } from "next/navigation";

interface Member {
  id: string;
  name: string;
  email: string;
  role: string;
}

const API_URL =
  "https://localhost:7248/api/Members";

export default function MembersPage() {
  const router = useRouter();

  // =====================================================
  // MEMBERS
  // =====================================================

  const [members, setMembers] =
    useState<Member[]>([]);

  const [loading, setLoading] =
    useState(true);

  // =====================================================
  // MESSAGES
  // =====================================================

  const [message, setMessage] =
    useState("");

  const [error, setError] =
    useState("");

  // =====================================================
  // ADD MEMBER
  // =====================================================

  const [showAddMember, setShowAddMember] =
    useState(false);

  const [memberName, setMemberName] =
    useState("");

  const [memberEmail, setMemberEmail] =
    useState("");

  const [memberPassword, setMemberPassword] =
    useState("");

  const [memberRole, setMemberRole] =
    useState("ANALYST");

  // =====================================================
  // CHANGE ROLE
  // =====================================================

  const [selectedMember, setSelectedMember] =
    useState<Member | null>(null);

  const [selectedRole, setSelectedRole] =
    useState("");

  const [changingRole, setChangingRole] =
    useState(false);

  // =====================================================
  // SAVING
  // =====================================================

  const [saving, setSaving] =
    useState(false);

  // =====================================================
  // CHECK ADMIN
  // =====================================================

  useEffect(() => {
    checkAdmin();
  }, []);

  const checkAdmin = () => {
    const token =
      localStorage.getItem("token");

    if (!token) {
      router.push("/login");
      return;
    }

    try {
      const payload = JSON.parse(
        atob(token.split(".")[1])
      );

      const role =
        payload[
          "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        ] ||
        payload.role ||
        "";

      if (role !== "ADMIN") {
        router.push("/dashboard");
        return;
      }

      loadMembers();

    } catch (err) {
      console.error(
        "Token error:",
        err
      );

      localStorage.removeItem("token");

      router.push("/login");
    }
  };

  // =====================================================
  // LOAD MEMBERS
  // =====================================================

  const loadMembers = async () => {
    setLoading(true);
    setError("");

    try {
      const token =
        localStorage.getItem("token");

      if (!token) {
        router.push("/login");
        return;
      }

      const response =
        await fetch(API_URL, {
          method: "GET",

          headers: {
            Authorization:
              `Bearer ${token}`,

            "Content-Type":
              "application/json",
          },
        });

      const responseText =
        await response.text();

      let data: any = [];

      if (responseText) {
        try {
          data = JSON.parse(
            responseText
          );
        } catch {
          data = [];
        }
      }

      if (!response.ok) {
        setError(
          data?.message ||
            `Unable to load members. Server returned ${response.status}.`
        );

        return;
      }

      setMembers(
        Array.isArray(data)
          ? data
          : []
      );

    } catch (err) {
      console.error(
        "Load Members Error:",
        err
      );

      setError(
        err instanceof Error
          ? err.message
          : "Unable to load members."
      );

    } finally {
      setLoading(false);
    }
  };

  // =====================================================
  // OPEN ADD MEMBER
  // =====================================================

  const openAddMemberModal = () => {
    setError("");
    setMessage("");

    setMemberName("");
    setMemberEmail("");
    setMemberPassword("");
    setMemberRole("ANALYST");

    setShowAddMember(true);
  };

  // =====================================================
  // CLOSE ADD MEMBER
  // =====================================================

  const closeAddMemberModal = () => {
    if (saving) {
      return;
    }

    setShowAddMember(false);

    setMemberName("");
    setMemberEmail("");
    setMemberPassword("");
    setMemberRole("ANALYST");

    setError("");
  };

  // =====================================================
  // CREATE MEMBER
  // =====================================================

  const handleCreateMember = async (
    e: FormEvent<HTMLFormElement>
  ) => {
    e.preventDefault();

    setError("");
    setMessage("");

    // Validation
    if (!memberName.trim()) {
      setError("Name is required.");
      return;
    }

    if (!memberEmail.trim()) {
      setError("Email is required.");
      return;
    }

    if (!memberPassword) {
      setError("Password is required.");
      return;
    }

    if (memberPassword.length < 6) {
      setError(
        "Password must contain at least 6 characters."
      );

      return;
    }

    if (!memberRole) {
      setError("Role is required.");
      return;
    }

    setSaving(true);

    try {
      const token =
        localStorage.getItem("token");

      if (!token) {
        router.push("/login");
        return;
      }

      // =================================================
      // IMPORTANT
      //
      // DO NOT send workspaceId.
      //
      // Backend gets workspaceId from JWT.
      // =================================================

      const response =
        await fetch(API_URL, {
          method: "POST",

          headers: {
            "Content-Type":
              "application/json",

            Authorization:
              `Bearer ${token}`,
          },

          body: JSON.stringify({
            name: memberName.trim(),

            email:
              memberEmail.trim(),

            password:
              memberPassword,

            role:
              memberRole,
          }),
        });

      const responseText =
        await response.text();

      console.log(
        "Create Member Status:",
        response.status
      );

      console.log(
        "Create Member Response:",
        responseText
      );

      let data: any = {};

      if (responseText) {
        try {
          data = JSON.parse(
            responseText
          );
        } catch {
          data = {
            message:
              responseText,
          };
        }
      }

      // =================================================
      // ERROR
      // =================================================

      if (!response.ok) {
        setError(
          data?.message ||
            `Unable to create member. Server returned ${response.status}.`
        );

        return;
      }

      // =================================================
      // SUCCESS
      // =================================================

      setMessage(
        data?.message ||
          "Member created successfully."
      );

      // Clear form
      setMemberName("");
      setMemberEmail("");
      setMemberPassword("");
      setMemberRole("ANALYST");

      // Close modal
      setShowAddMember(false);

      // Reload members
      await loadMembers();

    } catch (err) {
      console.error(
        "Create Member Error:",
        err
      );

      setError(
        err instanceof Error
          ? err.message
          : "Unable to create member."
      );

    } finally {
      setSaving(false);
    }
  };

  // =====================================================
  // OPEN CHANGE ROLE
  // =====================================================

  const openChangeRole = (
    member: Member
  ) => {
    setError("");
    setMessage("");

    setSelectedMember(member);

    setSelectedRole(
      member.role
    );
  };

  // =====================================================
  // CLOSE CHANGE ROLE
  // =====================================================

  const closeChangeRole = () => {
    if (changingRole) {
      return;
    }

    setSelectedMember(null);

    setSelectedRole("");

    setError("");
  };

  // =====================================================
  // CHANGE ROLE
  // =====================================================

  const handleChangeRole = async () => {
    if (!selectedMember) {
      return;
    }

    if (!selectedRole) {
      setError(
        "Please select a role."
      );

      return;
    }

    if (
      selectedRole ===
      selectedMember.role
    ) {
      setError(
        "Member already has this role."
      );

      return;
    }

    setChangingRole(true);

    setError("");
    setMessage("");

    try {
      const token =
        localStorage.getItem("token");

      if (!token) {
        router.push("/login");
        return;
      }

      const response =
        await fetch(
          `${API_URL}/${selectedMember.id}/role`,
          {
            method: "PUT",

            headers: {
              "Content-Type":
                "application/json",

              Authorization:
                `Bearer ${token}`,
            },

            body: JSON.stringify({
              role:
                selectedRole,
            }),
          }
        );

      const responseText =
        await response.text();

      console.log(
        "Change Role Status:",
        response.status
      );

      console.log(
        "Change Role Response:",
        responseText
      );

      let data: any = {};

      if (responseText) {
        try {
          data = JSON.parse(
            responseText
          );
        } catch {
          data = {
            message:
              responseText,
          };
        }
      }

      if (!response.ok) {
        setError(
          data?.message ||
            `Unable to change role. Server returned ${response.status}.`
        );

        return;
      }

      setMessage(
        data?.message ||
          "Member role updated successfully."
      );

      setSelectedMember(null);
      setSelectedRole("");

      await loadMembers();

    } catch (err) {
      console.error(
        "Change Role Error:",
        err
      );

      setError(
        err instanceof Error
          ? err.message
          : "Unable to change member role."
      );

    } finally {
      setChangingRole(false);
    }
  };

  // =====================================================
  // PAGE
  // =====================================================

  return (
    <div className="min-h-screen bg-gray-50 p-6">

      {/* =================================================
          HEADER
      ================================================= */}

      <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">

        <div>
          <h1 className="text-3xl font-bold text-gray-900">
            Members
          </h1>

          <p className="mt-1 text-sm text-gray-500">
            Manage members and their workspace roles.
          </p>
        </div>

        <button
          type="button"
          onClick={
            openAddMemberModal
          }
          className="rounded-lg bg-blue-600 px-5 py-3
                     font-semibold text-white
                     shadow-sm transition
                     hover:bg-blue-700"
        >
          + Add Member
        </button>

      </div>

      {/* =================================================
          SUCCESS
      ================================================= */}

      {message && (
        <div className="mb-5 rounded-lg
                        border border-green-200
                        bg-green-50 p-4">

          <p className="text-sm font-medium text-green-700">
            {message}
          </p>

        </div>
      )}

      {/* =================================================
          ERROR
      ================================================= */}

      {error && !showAddMember && (
        <div className="mb-5 rounded-lg
                        border border-red-200
                        bg-red-50 p-4">

          <p className="text-sm font-medium text-red-700">
            {error}
          </p>

        </div>
      )}

      {/* =================================================
          TABLE
      ================================================= */}

      <div className="overflow-hidden
                      rounded-xl
                      border border-gray-200
                      bg-white
                      shadow-sm">

        <div className="overflow-x-auto">

          <table className="min-w-full">

            <thead className="border-b
                              border-gray-200
                              bg-gray-50">

              <tr>

                <th className="px-6 py-4
                               text-left
                               text-xs
                               font-semibold
                               uppercase
                               tracking-wider
                               text-gray-500">
                  Name
                </th>

                <th className="px-6 py-4
                               text-left
                               text-xs
                               font-semibold
                               uppercase
                               tracking-wider
                               text-gray-500">
                  Email
                </th>

                <th className="px-6 py-4
                               text-left
                               text-xs
                               font-semibold
                               uppercase
                               tracking-wider
                               text-gray-500">
                  Role
                </th>

                <th className="px-6 py-4
                               text-right
                               text-xs
                               font-semibold
                               uppercase
                               tracking-wider
                               text-gray-500">
                  Action
                </th>

              </tr>

            </thead>

            <tbody className="divide-y
                              divide-gray-100">

              {/* Loading */}

              {loading && (
                <tr>

                  <td
                    colSpan={4}
                    className="px-6 py-12
                               text-center
                               text-sm
                               text-gray-500"
                  >
                    Loading members...
                  </td>

                </tr>
              )}

              {/* Empty */}

              {!loading &&
                members.length === 0 && (
                  <tr>

                    <td
                      colSpan={4}
                      className="px-6 py-12
                                 text-center
                                 text-sm
                                 text-gray-500"
                    >
                      No members found.
                    </td>

                  </tr>
                )}

              {/* Members */}

              {!loading &&
                members.length > 0 &&
                members.map(
                  (member) => (
                    <tr
                      key={
                        member.id
                      }
                      className="transition
                                 hover:bg-gray-50"
                    >

                      {/* Name */}

                      <td className="whitespace-nowrap
                                     px-6 py-4">

                        <div className="flex
                                        items-center
                                        gap-3">

                          <div className="flex
                                          h-10 w-10
                                          items-center
                                          justify-center
                                          rounded-full
                                          bg-blue-100">

                            <span className="font-semibold
                                             text-blue-700">

                              {member.name
                                .charAt(0)
                                .toUpperCase()}

                            </span>

                          </div>

                          <span className="font-medium
                                           text-gray-900">

                            {member.name}

                          </span>

                        </div>

                      </td>

                      {/* Email */}

                      <td className="whitespace-nowrap
                                     px-6 py-4
                                     text-sm
                                     text-gray-600">

                        {member.email}

                      </td>

                      {/* Role */}

                      <td className="whitespace-nowrap
                                     px-6 py-4">

                        <span
                          className={`inline-flex
                            rounded-full
                            px-3 py-1
                            text-xs
                            font-semibold
                            ${
                              member.role ===
                              "ADMIN"
                                ? "bg-purple-100 text-purple-700"
                                : member.role ===
                                  "ANALYST"
                                ? "bg-blue-100 text-blue-700"
                                : "bg-gray-100 text-gray-700"
                            }`}
                        >
                          {member.role}
                        </span>

                      </td>

                      {/* Action */}

                      <td className="whitespace-nowrap
                                     px-6 py-4
                                     text-right">

                        <button
                          type="button"
                          onClick={() =>
                            openChangeRole(
                              member
                            )
                          }
                          className="rounded-lg
                                     border
                                     border-gray-300
                                     px-4 py-2
                                     text-sm
                                     font-medium
                                     text-gray-700
                                     hover:bg-gray-50"
                        >
                          Change Role
                        </button>

                      </td>

                    </tr>
                  )
                )}

            </tbody>

          </table>

        </div>

      </div>

      {/* =================================================
          ADD MEMBER MODAL
      ================================================= */}

      {showAddMember && (
        <div
          className="fixed inset-0 z-50
                     flex items-center
                     justify-center
                     bg-black/40
                     px-4"
        >

          <div className="w-full max-w-md
                          rounded-2xl
                          bg-white
                          shadow-2xl">

            {/* Header */}

            <div className="border-b
                            border-gray-200
                            px-6 py-5">

              <div className="flex
                              items-center
                              justify-between">

                <div>

                  <h2 className="text-xl
                                 font-bold
                                 text-gray-900">
                    Add Member
                  </h2>

                  <p className="mt-1
                                text-sm
                                text-gray-500">
                    Create a new ANALYST or VIEWER.
                  </p>

                </div>

                <button
                  type="button"
                  onClick={
                    closeAddMemberModal
                  }
                  disabled={saving}
                  className="text-2xl
                             text-gray-400
                             hover:text-gray-700
                             disabled:opacity-50"
                >
                  ×
                </button>

              </div>

            </div>

            {/* Form */}

            <form
              onSubmit={
                handleCreateMember
              }
              className="space-y-5 p-6"
            >

              {/* Name */}

              <div>

                <label
                  htmlFor="memberName"
                  className="mb-2 block
                             text-sm
                             font-medium
                             text-gray-700"
                >
                  Name
                </label>

                <input
                  id="memberName"
                  type="text"
                  value={memberName}
                  onChange={(e) =>
                    setMemberName(
                      e.target.value
                    )
                  }
                  placeholder="Enter member name"
                  required
                  disabled={saving}
                  className="w-full
                             rounded-lg
                             border
                             border-gray-300
                             px-4 py-3
                             text-gray-900
                             outline-none
                             focus:border-blue-500
                             focus:ring-2
                             focus:ring-blue-200
                             disabled:bg-gray-100"
                />

              </div>

              {/* Email */}

              <div>

                <label
                  htmlFor="memberEmail"
                  className="mb-2 block
                             text-sm
                             font-medium
                             text-gray-700"
                >
                  Email
                </label>

                <input
                  id="memberEmail"
                  type="email"
                  value={memberEmail}
                  onChange={(e) =>
                    setMemberEmail(
                      e.target.value
                    )
                  }
                  placeholder="Enter member email"
                  required
                  disabled={saving}
                  className="w-full
                             rounded-lg
                             border
                             border-gray-300
                             px-4 py-3
                             text-gray-900
                             outline-none
                             focus:border-blue-500
                             focus:ring-2
                             focus:ring-blue-200
                             disabled:bg-gray-100"
                />

              </div>

              {/* Password */}

              <div>

                <label
                  htmlFor="memberPassword"
                  className="mb-2 block
                             text-sm
                             font-medium
                             text-gray-700"
                >
                  Password
                </label>

                <input
                  id="memberPassword"
                  type="password"
                  value={memberPassword}
                  onChange={(e) =>
                    setMemberPassword(
                      e.target.value
                    )
                  }
                  placeholder="Minimum 6 characters"
                  required
                  minLength={6}
                  disabled={saving}
                  className="w-full
                             rounded-lg
                             border
                             border-gray-300
                             px-4 py-3
                             text-gray-900
                             outline-none
                             focus:border-blue-500
                             focus:ring-2
                             focus:ring-blue-200
                             disabled:bg-gray-100"
                />

              </div>

              {/* Role */}

              <div>

                <label
                  htmlFor="memberRole"
                  className="mb-2 block
                             text-sm
                             font-medium
                             text-gray-700"
                >
                  Role
                </label>

                <select
                  id="memberRole"
                  value={memberRole}
                  onChange={(e) =>
                    setMemberRole(
                      e.target.value
                    )
                  }
                  disabled={saving}
                  className="w-full
                             rounded-lg
                             border
                             border-gray-300
                             bg-white
                             px-4 py-3
                             text-gray-900
                             outline-none
                             focus:border-blue-500
                             focus:ring-2
                             focus:ring-blue-200
                             disabled:bg-gray-100"
                >

                  <option value="ANALYST">
                    ANALYST
                  </option>

                  <option value="VIEWER">
                    VIEWER
                  </option>

                </select>

              </div>

              {/* Modal Error */}

              {error && (
                <div className="rounded-lg
                                border
                                border-red-200
                                bg-red-50
                                p-3">

                  <p className="text-sm
                                text-red-600">
                    {error}
                  </p>

                </div>
              )}

              {/* Buttons */}

              <div className="flex gap-3 pt-2">

                <button
                  type="button"
                  onClick={
                    closeAddMemberModal
                  }
                  disabled={saving}
                  className="flex-1
                             rounded-lg
                             border
                             border-gray-300
                             px-4 py-3
                             font-semibold
                             text-gray-700
                             hover:bg-gray-50
                             disabled:opacity-50"
                >
                  Cancel
                </button>

                <button
                  type="submit"
                  disabled={saving}
                  className="flex-1
                             rounded-lg
                             bg-blue-600
                             px-4 py-3
                             font-semibold
                             text-white
                             hover:bg-blue-700
                             disabled:cursor-not-allowed
                             disabled:opacity-60"
                >
                  {saving
                    ? "Creating..."
                    : "Create Member"}
                </button>

              </div>

            </form>

          </div>

        </div>
      )}

      {/* =================================================
          CHANGE ROLE MODAL
      ================================================= */}

      {selectedMember && (
        <div
          className="fixed inset-0 z-50
                     flex items-center
                     justify-center
                     bg-black/40
                     px-4"
        >

          <div className="w-full max-w-md
                          rounded-2xl
                          bg-white
                          shadow-2xl">

            {/* Header */}

            <div className="border-b
                            border-gray-200
                            px-6 py-5">

              <div className="flex
                              items-center
                              justify-between">

                <div>

                  <h2 className="text-xl
                                 font-bold
                                 text-gray-900">
                    Change Role
                  </h2>

                  <p className="mt-1
                                text-sm
                                text-gray-500">
                    {selectedMember.name}
                  </p>

                </div>

                <button
                  type="button"
                  onClick={
                    closeChangeRole
                  }
                  disabled={changingRole}
                  className="text-2xl
                             text-gray-400
                             hover:text-gray-700
                             disabled:opacity-50"
                >
                  ×
                </button>

              </div>

            </div>

            {/* Body */}

            <div className="space-y-5 p-6">

              {/* Email */}

              <div>

                <label className="mb-2 block
                                  text-sm
                                  font-medium
                                  text-gray-700">
                  Email
                </label>

                <div className="rounded-lg
                                bg-gray-50
                                px-4 py-3
                                text-sm
                                text-gray-600">
                  {selectedMember.email}
                </div>

              </div>

              {/* Role */}

              <div>

                <label
                  htmlFor="changeRole"
                  className="mb-2 block
                             text-sm
                             font-medium
                             text-gray-700"
                >
                  Role
                </label>

                <select
                  id="changeRole"
                  value={selectedRole}
                  onChange={(e) =>
                    setSelectedRole(
                      e.target.value
                    )
                  }
                  disabled={changingRole}
                  className="w-full
                             rounded-lg
                             border
                             border-gray-300
                             bg-white
                             px-4 py-3
                             text-gray-900
                             outline-none
                             focus:border-blue-500
                             focus:ring-2
                             focus:ring-blue-200"
                >

                  <option value="ADMIN">
                    ADMIN
                  </option>

                  <option value="ANALYST">
                    ANALYST
                  </option>

                  <option value="VIEWER">
                    VIEWER
                  </option>

                </select>

              </div>

              {/* Error */}

              {error && (
                <div className="rounded-lg
                                border
                                border-red-200
                                bg-red-50
                                p-3">

                  <p className="text-sm
                                text-red-600">
                    {error}
                  </p>

                </div>
              )}

              {/* Buttons */}

              <div className="flex gap-3 pt-2">

                <button
                  type="button"
                  onClick={
                    closeChangeRole
                  }
                  disabled={changingRole}
                  className="flex-1
                             rounded-lg
                             border
                             border-gray-300
                             px-4 py-3
                             font-semibold
                             text-gray-700
                             hover:bg-gray-50
                             disabled:opacity-50"
                >
                  Cancel
                </button>

                <button
                  type="button"
                  onClick={
                    handleChangeRole
                  }
                  disabled={
                    changingRole
                  }
                  className="flex-1
                             rounded-lg
                             bg-blue-600
                             px-4 py-3
                             font-semibold
                             text-white
                             hover:bg-blue-700
                             disabled:cursor-not-allowed
                             disabled:opacity-60"
                >
                  {changingRole
                    ? "Updating..."
                    : "Update Role"}
                </button>

              </div>

            </div>

          </div>

        </div>
      )}

    </div>
  );
}