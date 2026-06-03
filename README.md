## Prerequisites

To run this project on your machine, you need to have installed:
* **[SDK do .NET 10](https://dotnet.microsoft.com/download)** or newer.
* **[PostgreSQL](https://www.postgresql.org/download/)** or your local computer.
* An IDE such as Visual Studio 2022/2026, or Visual Studio Code.
---

## How to configure and run

1. **Clone the repository to your machine:**
   ```bash
   git clone https://github.com/SEU_USUARIO/Ticket-System.git
   cd Ticket-System
   ```

2. **Configure the Database (PostgreSQL)**  
   Open the `appsettings.json` file in the project root and make sure that the PostgreSQL connection information (such as your installation password) is correct. In the development example, the default password is set as `root`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=ticket_system_db;Username=postgres;Password=SENHA"
   }
   ```

3. **Restore the packages and apply the database**  
   Open the terminal (or the "Developer Command Prompt" tab) in the project's root folder and run:
   ```bash
   dotnet build
   dotnet ef database update
   ```
   *(Note: The project also contains a routine in the Program.cs file to perform migrations automatically when the project starts).*

4. **Start the system**
   ```bash
   dotnet run
   ```
   Or simply click on "Run / F5" in your Visual Studio.

5. **Acess the System:**  
   Open your browser and access the URL that will appear in the Console (e.g., `https://localhost:7145`).

6. **Initial Step (Administration):**
   When starting the system for the first time, the first step must be to access the administrator profile. Log in with the default credentials below and proceed with creating the initial users who will use the system:
   * **User:** `admin`
   * **Password:** `admin`

---

## Technologies Used
* C# .NET 10
* ASP.NET Core MVC e Web API
* Entity Framework Core (Code-First)
* PostgreSQL Database
* Bootstrap 5 (Front-end local) and jQuery
