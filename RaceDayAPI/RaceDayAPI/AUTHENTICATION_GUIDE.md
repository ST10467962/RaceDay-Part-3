# Authentication and Access Control

This describes how RaceDayAPI signs users in, keeps them signed in, and decides what they can do.

## Registration

`POST /api/Auth/register` creates an account. The body contains `firstName`, `lastName`, `email`, `password`, `phoneNumber`, `role` and, depending on the role, the fields below.

| Role | Extra requirements |
| --- | --- |
| `Participant` | `dateOfBirth` (required, must be in the past). `gender` is optional. A participant profile is created at the same time. |
| `Organiser` | `organiserAccessCode` (required). Must match `OrganiserRegistration:AccessCode` in configuration. If no code is configured, organiser registration is closed. |

The code is compared in constant time. Configure it per environment:

- Development: `appsettings.Development.json` (`RACEDAY-ORGANISER`).
- Anywhere else: user secrets or an environment variable, for example `OrganiserRegistration__AccessCode`.

On success the new user is signed in straight away.

## Passwords

Passwords are never stored in plain text. Each new password is hashed with PBKDF2-SHA256 (100,000 iterations) using a random 16-byte salt. The stored value has the form `v1.{iterations}.{salt}.{hash}`.

Accounts created in Part 2 hold an unsalted SHA-256 hash. These still verify. On their next successful login the hash is replaced with the PBKDF2 format.

## Login and sessions

`POST /api/Auth/login` checks the email and password. On success the API issues:

- A cookie authentication ticket (`.AspNetCore.Cookies`), valid for 60 minutes with sliding expiry.
- A server-side session (`.RaceDay.Session`) holding the user id, role and email.

The role comes from the stored user record at login time. Each request is authorised against the claims in the cookie. `POST /api/Auth/logout` ends both.

The MVC application keeps these cookies in its own per-visitor session and replays them on each call to the API.

## Access rules

| Area | Rule |
| --- | --- |
| Every endpoint except `Auth/register` and `Auth/login` | Requires a signed-in user (`[Authorize]`). Anonymous requests get `401`. |
| Creating, updating or deleting events, categories, and banners | Organiser only, and only for events the organiser owns. Otherwise `403`. |
| Enrolling | Participant only. The participant is always the signed-in user. A participant can enrol once per event (cancelled enrolments don't count) and only before the event date. |
| Withdrawing an enrolment | Participant may cancel their own enrolment before the event date. |
| Confirming, cancelling or setting an enrolment to Pending | Organiser only, for their own events. |
| Capturing results | Organiser only, for their own events, on or after the event date. |
| Viewing results and enrolments | Organisers see their own events. Participants see only their own entries and results. |
| Profile | Each user reads and updates only their own profile. |
| Images (`GET api/Files/{blobName}`) | Signed-in users only. Only `events/` and `profiles/` blobs can be read. |

The MVC application enforces navigation by role as well. The API is the authority: the MVC never relies on its own checks to protect data.

## Endpoint summary

| Endpoint | Method | Who |
| --- | --- | --- |
| `/api/Auth/register` | POST | Anyone |
| `/api/Auth/login` | POST | Anyone |
| `/api/Auth/logout` | POST | Signed in |
| `/api/Events` | GET, POST | Signed in; POST is Organiser |
| `/api/Events/{id}` | GET, PUT, DELETE | Signed in; PUT and DELETE are owner Organiser |
| `/api/Events/{id}/banner` | POST | Owner Organiser |
| `/api/Categories` | GET, POST, PUT, DELETE | Signed in; writes are owner Organiser |
| `/api/Entries` | GET, POST | Signed in; POST is Participant |
| `/api/Entries/{id}/status` | PUT | Owner Organiser, or Participant (Cancelled only) |
| `/api/Results` | GET | Signed in, scoped to the caller |
| `/api/Results/event/{eventId}` | POST | Owner Organiser |
| `/api/Profile` | GET, PUT | Signed in (own profile) |
| `/api/Profile/picture` | POST | Signed in (own profile) |
| `/api/Files/{blobName}` | GET | Signed in |
