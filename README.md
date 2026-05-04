## Pré-requisitos

Para rodar este projeto na sua máquina, você precisa ter instalado:
* **[SDK do .NET 10](https://dotnet.microsoft.com/download)** ou mais recente.
* **[PostgreSQL](https://www.postgresql.org/download/)** no seu computador local.
* Uma IDE como Visual Studio 2022/2026, ou Visual Studio Code.

---

## Como configurar e rodar

1. **Clone o repositório** para a sua máquina:
   ```bash
   git clone https://github.com/SEU_USUARIO/Ticket-System.git
   cd Ticket-System
   ```

2. **Configure o Banco de Dados (PostgreSQL)**  
   Abra o arquivo `appsettings.json` na raiz do projeto e certifique-se de que as informações de conexão do PostgreSQL (como sua senha de instalação) estão corretas. No exemplo de desenvolvimento a senha padrão é tida como `root`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=ticket_system_db;Username=postgres;Password=SENHA"
   }
   ```

3. **Restaure os pacotes e aplique o banco de dados**  
   Abra o terminal (ou a aba de "Developer Command Prompt") na pasta raiz do projeto e execute:
   ```bash
   dotnet build
   dotnet ef database update
   ```
   *(Obs: O projeto também contém uma rotina em no arquivo Program.cs para efetuar as migrações automaticamente quando o projeto inicia).*

4. **Inicie o sistema**
   ```bash
   dotnet run
   ```
   Ou simplesmente clique em **"Run / F5"** no seu Visual Studio.

5. **Acesse o Sistema:**  
   Abra o seu navegador e acesse a URL que aparecerá no Console (ex: `https://localhost:7145`).

---

## Tecnologias Utilizadas
* C# .NET 10
* ASP.NET Core MVC e Web API
* Entity Framework Core (Code-First)
* Banco de Dados PostgreSql
* Bootstrap 5 (Front-end local) e jQuery