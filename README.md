# ?? Recipe World - ASP.NET Core MVC Recipe Management System

A full-featured recipe management web application built with ASP.NET Core 8.0, featuring user authentication, recipe management, ingredient tracking, and admin approval workflows.

## ? Features

- **User Authentication & Authorization**
  - ASP.NET Core Identity with role-based access
  - User registration and login
  - Email confirmation
  - Two-factor authentication support

- **Recipe Management**
- Create, read, update, and delete recipes
- Recipe status workflow (Draft, Public, Private, Pending)
- Rich recipe details including prep time, cook time, servings, and temperature
- Step-by-step cooking instructions
- Recipe categorization
- **Image upload functionality** - Upload recipe photos or use image URLs

- **Ingredient Management**
  - Comprehensive ingredient database
  - Ingredient types and details
  - Link ingredients to recipes with amounts and units
  - Admin approval system for new ingredients

- **Search & Filter**
  - Search recipes by name or description
  - Filter by category and status
  - Pagination support

- **Responsive Design**
  - Mobile-friendly interface
  - Modern Bootstrap-based UI
  - Card-based recipe display

## ?? Technologies Used

- **Backend**: ASP.NET Core 8.0 MVC
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **Frontend**: Razor Views, Bootstrap 5
- **API Documentation**: Swagger/OpenAPI
- **Languages**: C# 12.0, HTML, CSS, JavaScript

## ?? Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (LocalDB, Express, or full version)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)

## ?? Setup Instructions

### 1. Clone the Repository

```bash
git clone <your-repository-url>
cd worldRecipeMvc
```

### 2. Configure Database Connection

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "RecipeWorldConnectionString": "Server=(localdb)\\mssqllocaldb;Database=RecipeWorldDB;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### 3. Apply Database Migrations

Open a terminal in the project directory and run:

```bash
cd src/worldRecipeMvc
dotnet ef database update
```

This will:
- Create the database
- Apply all migrations
- Seed sample data including:
  - 6 recipe categories
  - 23 common ingredients
  - 6 delicious recipes 
  - A demo user account

### 4. Run the Application

```bash
dotnet run
```

Or press `F5` in Visual Studio.

The application will be available at:
- HTTPS: `https://localhost:7001`
- HTTP: `http://localhost:5000`
- Swagger API: `https://localhost:7001/swagger`

## Demo Accounts

For testing purposes, the following accounts are seeded:

- **Regular User**: demo@recipeworld.com / Demo123!
- **Admin User**: admin@recipeworld.com / Admin123!

⚠️ **Note**: Change these credentials in production environments.

## ?? Sample Data

The application includes pre-populated data:

### Recipes Included:
1. **Brigadeiro** ???? - Traditional Brazilian chocolate truffle dessert
2. **Spaghetti Carbonara** ???? - Classic Italian pasta dish
3. **Chicken Tikka Masala** ???? - Popular Indian curry
4. **Margherita Pizza** ???? - Traditional Italian pizza
5. **Classic Pancakes** ?? - American breakfast favorite
6. **Caesar Salad** ?? - Fresh and crispy salad

### Categories:
- Desserts
- Main Course
- Appetizers
- Soups
- Salads
- Breakfast

## ??? Project Structure

```
worldRecipeMvc/
??? Areas/
?   ??? Identity/          # Identity scaffolded pages
??? Controllers/           # MVC Controllers
??? Data/                  # Database context and migrations
?   ??? ApplicationDbContext.cs
?   ??? SeedData.cs
?   ??? Migrations/
??? Models/                # Domain models
?   ??? Recipe.cs
?   ??? Ingredient.cs
?   ??? Category.cs
?   ??? ViewModels/
??? Services/              # Business logic layer
?   ??? RecipeService.cs
?   ??? IngredientService.cs
?   ??? CategoryService.cs
??? Views/                 # Razor views
?   ??? Home/
?   ??? Recipes/
?   ??? Shared/
??? wwwroot/              # Static files (CSS, JS, images)
```

## ?? Key Features for Portfolio

This project demonstrates:

- ? **Clean Architecture** - Separation of concerns with Services, Controllers, and Models
- ? **Entity Framework Core** - Code-first approach with migrations
- ? **Repository Pattern** - Service layer abstraction
- ? **Authentication & Authorization** - ASP.NET Core Identity implementation
- ? **CRUD Operations** - Complete create, read, update, delete functionality
- ? **Async/Await** - Asynchronous programming throughout
- ? **Dependency Injection** - Built-in DI container usage
- ? **Error Handling** - Custom middleware and logging
- ? **API Documentation** - Swagger/OpenAPI integration
- ? **Responsive Design** - Mobile-first approach
- ? **Data Validation** - Model validation with data annotations
- ? **Many-to-Many Relationships** - Recipe-Ingredient relationship

## ?? Troubleshooting

### Database Connection Issues

If you encounter database connection errors:

1. Ensure SQL Server is running
2. Verify the connection string in `appsettings.json`
3. Try resetting the database:
   ```bash
   dotnet ef database drop
   dotnet ef database update
   ```

### Migration Issues

If migrations fail:

```bash
dotnet ef migrations remove
dotnet ef migrations add InitialCreate
dotnet ef database update
```


## ?? Contributing

This is a portfolio project, but suggestions and feedback are welcome! Feel free to open an issue or submit a pull request.

## ?? License

This project is open source and available for educational purposes.

## ?? Author

Created as a portfolio project to demonstrate full-stack web development skills with ASP.NET Core.

---

### ?? Contact & Links

- **GitHub**: [[My GitHub Profile](https://github.com/MatheusFerraro)]
- **LinkedIn**: [My LinkedIn Profile](https://www.linkedin.com/in/mcamiloferraro/)
- **Email**: [My Email](matheus.ferraro@gmail.com)

---


