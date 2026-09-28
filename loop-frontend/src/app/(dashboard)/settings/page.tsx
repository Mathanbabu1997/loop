"use client";

import { useEffect, useState } from "react";

const API_URL = "https://loop-api-s464.onrender.com/api/Settings";

interface Profile {
  id: string;
  name: string;
  email: string;
  role: string;
  workspaceId: string;
  workspaceName: string;
}

export default function SettingsPage() {

  // =========================================================
  // STATE
  // =========================================================

  const [profile, setProfile] =
    useState<Profile | null>(null);

  const [name, setName] = useState("");

  const [currentPassword, setCurrentPassword] =
    useState("");

  const [newPassword, setNewPassword] =
    useState("");

  const [confirmPassword, setConfirmPassword] =
    useState("");


  const [loadingProfile, setLoadingProfile] =
    useState(true);

  const [savingProfile, setSavingProfile] =
    useState(false);

  const [changingPassword, setChangingPassword] =
    useState(false);


  const [profileMessage, setProfileMessage] =
    useState("");

  const [passwordMessage, setPasswordMessage] =
    useState("");


  const [profileError, setProfileError] =
    useState("");

  const [passwordError, setPasswordError] =
    useState("");


  // =========================================================
  // LOAD PROFILE
  // =========================================================

  useEffect(() => {
    loadProfile();
  }, []);


  const loadProfile = async () => {

    try {

      setLoadingProfile(true);

      setProfileError("");

      const token =
        localStorage.getItem("token");


      if (!token) {

        window.location.href = "/login";

        return;
      }


      const response = await fetch(
        `${API_URL}/profile`,
        {
          method: "GET",

          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );


      if (response.status === 401) {

        localStorage.removeItem("token");

        window.location.href = "/login";

        return;
      }


      const data =
        await response.json();


      if (!response.ok) {

        throw new Error(
          data.message ||
          "Unable to load profile."
        );
      }


      setProfile(data);

      setName(data.name);

    }
    catch (error: any) {

      setProfileError(
        error.message ||
        "Unable to load profile."
      );

    }
    finally {

      setLoadingProfile(false);

    }
  };


  // =========================================================
  // SAVE PROFILE
  // =========================================================

  const saveProfile = async () => {

    setProfileMessage("");

    setProfileError("");


    // =======================================================
    // FRONTEND NAME VALIDATION
    // =======================================================

    const trimmedName =
      name.trim();


    if (!trimmedName) {

      setProfileError(
        "Name cannot be empty."
      );

      return;
    }


    if (trimmedName.length < 2) {

      setProfileError(
        "Name must be at least 2 characters."
      );

      return;
    }


    if (trimmedName.length > 100) {

      setProfileError(
        "Name cannot exceed 100 characters."
      );

      return;
    }


    // =======================================================
    // CHECK SAME NAME
    // =======================================================

    if (
      profile?.name.trim().toLowerCase() ===
      trimmedName.toLowerCase()
    ) {

      setProfileError(
        "No changes were made."
      );

      return;
    }


    try {

      setSavingProfile(true);

      const token =
        localStorage.getItem("token");


      if (!token) {

        window.location.href = "/login";

        return;
      }


      const response = await fetch(
        `${API_URL}/profile`,
        {
          method: "PUT",

          headers: {
            "Content-Type":
              "application/json",

            Authorization:
              `Bearer ${token}`,
          },

          body: JSON.stringify({
            name: trimmedName,
          }),
        }
      );


      if (response.status === 401) {

        localStorage.removeItem("token");

        window.location.href = "/login";

        return;
      }


      const data =
        await response.json();


      if (!response.ok) {

        throw new Error(
          data.message ||
          "Unable to update profile."
        );
      }


      setProfile(
        (previous) =>
          previous
            ? {
                ...previous,
                name: data.name,
              }
            : previous
      );


      setName(data.name);


      setProfileMessage(
        "Profile updated successfully."
      );

    }
    catch (error: any) {

      setProfileError(
        error.message ||
        "Unable to update profile."
      );

    }
    finally {

      setSavingProfile(false);

    }
  };


  // =========================================================
  // CHANGE PASSWORD
  // =========================================================

  const changePassword = async () => {

    setPasswordMessage("");

    setPasswordError("");


    // =======================================================
    // CURRENT PASSWORD
    // =======================================================

    if (!currentPassword.trim()) {

      setPasswordError(
        "Current password is required."
      );

      return;
    }


    // =======================================================
    // NEW PASSWORD
    // =======================================================

    if (!newPassword.trim()) {

      setPasswordError(
        "New password is required."
      );

      return;
    }


    // =======================================================
    // MINIMUM PASSWORD LENGTH
    // =======================================================

    if (newPassword.length < 8) {

      setPasswordError(
        "New password must be at least 8 characters."
      );

      return;
    }


    // =======================================================
    // MAXIMUM PASSWORD LENGTH
    // =======================================================

    if (newPassword.length > 100) {

      setPasswordError(
        "New password cannot exceed 100 characters."
      );

      return;
    }


    // =======================================================
    // SAME PASSWORD CHECK
    // =======================================================

    if (newPassword === currentPassword) {

      setPasswordError(
        "New password cannot be the same as your current password."
      );

      return;
    }


    // =======================================================
    // CONFIRM PASSWORD
    // =======================================================

    if (!confirmPassword.trim()) {

      setPasswordError(
        "Confirm password is required."
      );

      return;
    }


    if (
      newPassword !==
      confirmPassword
    ) {

      setPasswordError(
        "New password and confirmation password do not match."
      );

      return;
    }


    // =======================================================
    // API REQUEST
    // =======================================================

    try {

      setChangingPassword(true);


      const token =
        localStorage.getItem("token");


      if (!token) {

        window.location.href = "/login";

        return;
      }


      const response = await fetch(
        `${API_URL}/password`,
        {
          method: "PUT",

          headers: {
            "Content-Type":
              "application/json",

            Authorization:
              `Bearer ${token}`,
          },

          body: JSON.stringify({
            currentPassword,
            newPassword,
            confirmPassword,
          }),
        }
      );


      if (response.status === 401) {

        localStorage.removeItem("token");

        window.location.href = "/login";

        return;
      }


      const data =
        await response.json();


      if (!response.ok) {

        throw new Error(
          data.message ||
          "Unable to change password."
        );
      }


      // =====================================================
      // CLEAR PASSWORD FIELDS
      // =====================================================

      setCurrentPassword("");

      setNewPassword("");

      setConfirmPassword("");


      setPasswordMessage(
        "Password changed successfully."
      );

    }
    catch (error: any) {

      setPasswordError(
        error.message ||
        "Unable to change password."
      );

    }
    finally {

      setChangingPassword(false);

    }
  };


  // =========================================================
  // LOGOUT
  // =========================================================

  const logout = () => {

    localStorage.removeItem("token");

    window.location.href = "/login";
  };


  // =========================================================
  // LOADING
  // =========================================================

  if (loadingProfile) {

    return (
      <div className="min-h-screen bg-gray-50 p-8">

        <div className="mx-auto max-w-5xl">

          <div className="rounded-xl bg-white p-8 shadow-sm">

            <p className="text-gray-500">
              Loading settings...
            </p>

          </div>

        </div>

      </div>
    );
  }


  // =========================================================
  // PAGE
  // =========================================================

  return (

    <div className="min-h-screen bg-gray-50 p-8">

      <div className="mx-auto max-w-5xl">


        {/* ===================================================
            HEADER
        =================================================== */}

        <div className="mb-8">

          <h1 className="text-3xl font-bold text-gray-900">
            Settings
          </h1>

          <p className="mt-1 text-gray-500">
            Manage your profile, security and workspace
            settings.
          </p>

        </div>


        {/* ===================================================
            PROFILE
        =================================================== */}

        <div className="mb-6 rounded-xl bg-white p-6 shadow-sm">

          <div className="mb-6">

            <h2 className="text-xl font-semibold text-gray-900">
              Profile
            </h2>

            <p className="mt-1 text-sm text-gray-500">
              Update your personal information.
            </p>

          </div>


          {/* PROFILE ERROR */}

          {profileError && (

            <div className="mb-4 rounded-lg bg-red-50 px-4 py-3 text-sm text-red-700">

              {profileError}

            </div>

          )}


          {/* PROFILE SUCCESS */}

          {profileMessage && (

            <div className="mb-4 rounded-lg bg-green-50 px-4 py-3 text-sm text-green-700">

              {profileMessage}

            </div>

          )}


          <div className="grid grid-cols-1 gap-5 md:grid-cols-2">


            {/* NAME */}

            <div>

              <label className="mb-2 block text-sm font-medium text-gray-700">

                Name

              </label>

              <input
                type="text"
                value={name}
                maxLength={100}
                onChange={(e) => {

                  setName(e.target.value);

                  setProfileError("");

                  setProfileMessage("");

                }}
                placeholder="Enter your name"
                className="w-full rounded-lg border border-gray-300 px-4 py-3 text-gray-900 outline-none transition focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
              />

              <p className="mt-1 text-xs text-gray-400">
                2–100 characters
              </p>

            </div>


            {/* EMAIL */}

            <div>

              <label className="mb-2 block text-sm font-medium text-gray-700">

                Email

              </label>

              <input
                type="text"
                value={
                  profile?.email || ""
                }
                disabled
                className="w-full cursor-not-allowed rounded-lg border border-gray-200 bg-gray-100 px-4 py-3 text-gray-500"
              />

            </div>


            {/* ROLE */}

            <div>

              <label className="mb-2 block text-sm font-medium text-gray-700">

                Role

              </label>

              <input
                type="text"
                value={
                  profile?.role || ""
                }
                disabled
                className="w-full cursor-not-allowed rounded-lg border border-gray-200 bg-gray-100 px-4 py-3 text-gray-500"
              />

            </div>


            {/* WORKSPACE */}

            <div>

              <label className="mb-2 block text-sm font-medium text-gray-700">

                Workspace

              </label>

              <input
                type="text"
                value={
                  profile?.workspaceName ||
                  profile?.workspaceId ||
                  ""
                }
                disabled
                className="w-full cursor-not-allowed rounded-lg border border-gray-200 bg-gray-100 px-4 py-3 text-gray-500"
              />

            </div>

          </div>


          {/* SAVE */}

          <div className="mt-6">

            <button
              onClick={saveProfile}
              disabled={savingProfile}
              className="rounded-lg bg-blue-600 px-5 py-3 font-medium text-white transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
            >

              {savingProfile
                ? "Saving..."
                : "Save Changes"}

            </button>

          </div>

        </div>


        {/* ===================================================
            SECURITY
        =================================================== */}

        <div className="mb-6 rounded-xl bg-white p-6 shadow-sm">

          <div className="mb-6">

            <h2 className="text-xl font-semibold text-gray-900">
              Security
            </h2>

            <p className="mt-1 text-sm text-gray-500">
              Change your account password.
            </p>

          </div>


          {/* PASSWORD ERROR */}

          {passwordError && (

            <div className="mb-4 rounded-lg bg-red-50 px-4 py-3 text-sm text-red-700">

              {passwordError}

            </div>

          )}


          {/* PASSWORD SUCCESS */}

          {passwordMessage && (

            <div className="mb-4 rounded-lg bg-green-50 px-4 py-3 text-sm text-green-700">

              {passwordMessage}

            </div>

          )}


          <div className="space-y-5">


            {/* CURRENT PASSWORD */}

            <div>

              <label className="mb-2 block text-sm font-medium text-gray-700">

                Current Password

              </label>

              <input
                type="password"
                value={currentPassword}
                onChange={(e) => {

                  setCurrentPassword(
                    e.target.value
                  );

                  setPasswordError("");

                  setPasswordMessage("");

                }}
                placeholder="Enter current password"
                autoComplete="current-password"
                className="w-full rounded-lg border border-gray-300 px-4 py-3 text-gray-900 outline-none transition focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
              />

            </div>


            {/* NEW PASSWORD */}

            <div>

              <label className="mb-2 block text-sm font-medium text-gray-700">

                New Password

              </label>

              <input
                type="password"
                value={newPassword}
                maxLength={100}
                onChange={(e) => {

                  setNewPassword(
                    e.target.value
                  );

                  setPasswordError("");

                  setPasswordMessage("");

                }}
                placeholder="Enter new password"
                autoComplete="new-password"
                className="w-full rounded-lg border border-gray-300 px-4 py-3 text-gray-900 outline-none transition focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
              />

              <p className="mt-1 text-xs text-gray-400">
                Minimum 8 characters
              </p>

            </div>


            {/* CONFIRM PASSWORD */}

            <div>

              <label className="mb-2 block text-sm font-medium text-gray-700">

                Confirm New Password

              </label>

              <input
                type="password"
                value={confirmPassword}
                maxLength={100}
                onChange={(e) => {

                  setConfirmPassword(
                    e.target.value
                  );

                  setPasswordError("");

                  setPasswordMessage("");

                }}
                placeholder="Confirm new password"
                autoComplete="new-password"
                className="w-full rounded-lg border border-gray-300 px-4 py-3 text-gray-900 outline-none transition focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
              />

            </div>

          </div>


          {/* CHANGE PASSWORD */}

          <div className="mt-6">

            <button
              onClick={changePassword}
              disabled={changingPassword}
              className="rounded-lg bg-gray-900 px-5 py-3 font-medium text-white transition hover:bg-gray-800 disabled:cursor-not-allowed disabled:opacity-60"
            >

              {changingPassword
                ? "Changing..."
                : "Change Password"}

            </button>

          </div>

        </div>


        {/* ===================================================
            WORKSPACE
        =================================================== */}

        <div className="mb-6 rounded-xl bg-white p-6 shadow-sm">

          <h2 className="text-xl font-semibold text-gray-900">
            Workspace
          </h2>

          <p className="mt-1 text-sm text-gray-500">
            Workspace information for your account.
          </p>


          <div className="mt-5">

            <label className="mb-2 block text-sm font-medium text-gray-700">

              Workspace ID

            </label>

            <input
              type="text"
              value={
                profile?.workspaceId || ""
              }
              disabled
              className="w-full cursor-not-allowed rounded-lg border border-gray-200 bg-gray-100 px-4 py-3 text-sm text-gray-500"
            />

          </div>

        </div>


        {/* ===================================================
            ACCOUNT
        =================================================== */}

        <div className="rounded-xl bg-white p-6 shadow-sm">

          <h2 className="text-xl font-semibold text-gray-900">
            Account
          </h2>

          <p className="mt-1 text-sm text-gray-500">
            Sign out from your LOOP account.
          </p>


          <button
            onClick={logout}
            className="mt-5 rounded-lg border border-red-300 px-5 py-3 font-medium text-red-600 transition hover:bg-red-50"
          >
            Logout
          </button>

        </div>

      </div>

    </div>
  );
}