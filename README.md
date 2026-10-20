# StudentPortlaSystem
# 🏫 School Management System

A modern, full-stack school administration and student management platform designed to streamline academic operations, automated student registration, real-time analytics, and secure digital identification.

---

## 🚀 Key Features

* **Admin & Teacher Dashboards:** Role-based access control providing administrators with school-wide overview metrics and teachers with class and student performance analytics.
* **Streamlined Student Registration:** Comprehensive registration workflows supporting grade levels (Nursery through Grade 12), sections (A–F), core subject tracking, and student photo uploads.
* **Smart ID Card Generation:** Automated digital ID card generation featuring unique serial tracking, student metadata, and QR code verification for secure campus identification.
* **Automated Database Seeding:** Intelligent startup initialization that automatically verifies and seeds essential academic master data, grade structures, and initial administrative accounts.
* **Robust Error & Exception Handling:** Production-ready exception logging and execution strategy patterns ensuring data integrity against transient database connection drops.

---

## 🛠️ Tech Stack

### Backend
* **Framework:** ASP.NET Core 8.0 Web API (C#)
* **Database & ORM:** PostgreSQL (hosted on Aiven) with Entity Framework Core (`Npgsql.EntityFrameworkCore.PostgreSQL`)
* **Hosting:** Render

### Frontend
* **Library:** React (Vite)
* **Styling:** Tailwind CSS / Modern CSS
* **HTTP Client:** Axios
* **Hosting:** Vercel

---

## ⚙️ Getting Started & Local Setup

### Prerequisites
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed on your machine.
* [Node.js (v18 or higher)](https://nodejs.org/) installed for the frontend.
* A PostgreSQL instance (local or cloud-hosted).

### 1. Clone the Repository
```bash
git clone [https://github.com/degifetise/SchoolMgmtSystem-halade.git](https://github.com/degifetise/SchoolMgmtSystem-halade.git)
cd SchoolMgmtSystem-halade

2. Configure Backend Environment
Navigate to the backend directory and configure your connection string in appsettings.json or user secrets:

JSON
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=your_host;Database=your_db;Username=your_user;Password=your_password"
  }
}


3. Run the Backend API
Bash
cd backend/HaladeHighSchool.Api
dotnet restore
dotnet run


4. Run the Frontend
Open a new terminal tab, navigate to the frontend directory, install dependencies, and start the development server:

Bash
cd frontend
npm install
npm run dev


📂 Project Architecture
Plaintext
SchoolMgmtSystem-halade/
├── backend/
│   └── HaladeHighSchool.Api/
│       ├── Controllers/      # API Endpoints (Admin, Teacher, SmartID, Auth)
│       ├── Data/             # EF Core DbContext & Database Seeders
│       ├── Models/           # Database Entities & DTOs
│       └── Services/         # Business Logic (SmartIDCardService, etc.)
└── frontend/
    └── src/
        ├── components/       # Reusable UI Components & Navigation
        ├── pages/            # Admin Dashboards, SmartID Print Pages, Forms
        └── services/         # Axios API Integration Endpoints


👨‍💻 Author
Developed by Degife Tise

Full-Stack Software Developer

GitHub: @degifetise

📄 License
This project is open-source and available under the MIT License.


