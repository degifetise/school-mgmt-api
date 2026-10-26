School Management & Smart ID System 🏫🚀

A production-ready, full-stack school administration and smart identification platform designed to streamline academic operations, multi-tenant branding, public student registration workflows, and secure digital ID generation.

📋 Table of Contents

At a Glance

Project Architecture & Structure

Core Features

Tech Stack

Getting Started & Prerequisites

Environment Configuration

Running the Application Locally

Deployment

License

🔍 At a Glance

Purpose: Manage school administration and student lifecycle tasks such as public registration, tracking, reporting, and secure digital ID generation.

Backend: ASP.NET Core Web API in C#.

Database: PostgreSQL with Entity Framework Core.

Frontend: React (Vite) with Tailwind CSS and Axios.

Hosting: Render (Backend API) & Vercel (Frontend Client).

📂 Project Architecture & Structure

This repository is organized as a monorepo containing both client and server applications:

├── backend/HaladeHighSchool.Api/   # ASP.NET Core API (Controllers, DTOs, Services, EF Core DB Context)
├── frontend/                       # React Vite application (UI components, Admin settings, Tailwind styles)
├── database/                       # Database scripts and demo seeding logic
├── docs/                           # Project documentation, architecture notes, and guides
└── uploads/                        # Secure local storage assets (school logos, profile pictures)



✨ Core Feature Areas

Multi-Tenant Administration & Branding: School-specific branding configurations (LogoUrl validation supporting JPEG/PNG/WebP up to 2MB).

Public Registration Toggles: Configurable admission routes (AllowPublicRegistration) featuring robust 403-Forbidden response handling when closed.

Student Lifecycle Management: Comprehensive dashboards for administrators and teachers to manage student records, grades, and sections.

Automated PVC Smart ID Generation: Dual-sided digital ID card generation equipped with dynamic QR code verification.

Resilient API Handling: Global Axios retry interceptors with exponential backoff and shared reconnect states to manage server cold-starts seamlessly.

Database Seeding: Initial master data scripts and default administrator account seeding for fast setup.

🛠️ Tech Stack

Frontend

React (Vite): Fast, modern single-page application UI.

Tailwind CSS: Utility-first styling for responsive layouts.

Axios: HTTP client configured with interceptors for error management and retry strategies.

Backend

ASP.NET Core Web API: Robust, high-performance C# backend architecture.

Entity Framework Core (EF Core): Object-Relational Mapper (ORM) for data modeling and migrations.

PostgreSQL: Reliable relational database storage.

⚙️ Getting Started & Prerequisites

Make sure you have the following installed on your local machine:

.NET SDK (latest version)

Node.js & npm (LTS recommended)

PostgreSQL database server instance

🔐 Environment Configuration

In the backend/HaladeHighSchool.Api/ directory, create an appsettings.json or appsettings.Development.json file configuring your database connection string:

{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=SchoolManagementDb;Username=postgres;Password=yourpassword"
  }
}


In the frontend/ directory, set up your API base URL environment variable if necessary (e.g., .env):

VITE_API_BASE_URL=http://localhost:5006


🚀 Running the Application Locally

1. Run the Backend API

cd backend/HaladeHighSchool.Api
dotnet restore
dotnet ef database update
dotnet run


The backend API will run locally at http://localhost:5006.

2. Run the Frontend Client

Open a new terminal window:

cd frontend
npm install
npm run dev


The React application will run locally at http://localhost:5173.

🌐 Deployment

Backend: Deployed as a Web Service on Render.

Frontend: Deployed as a static site on Vercel.

⚖️ License

© 2026 All Rights Reserved.

This repository and its source code are proprietary. Unauthorized copying, downloading, modifying, public sharing, commercial use, or forking of this repository—via any medium—is strictly prohibited without the express written consent of the author.
