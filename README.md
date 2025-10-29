# TownSquare - Local Events Board

TownSquare is a central hub where community members can discover and share local events—markets, concerts, workshops, charity runs, and more. The application encourages engagement and community connection through an intuitive, user-friendly experience.

## Features

### Event Management System
- **Full CRUD Operations**: Authenticated users can create, read, update, and delete their events
- **Event Details**: Each event includes title, description, date, time, location, and category
- **Search & Filtering**: Search by keywords, filter by category, or date range
- **Categories**: Market, Concert, Workshop, Charity, Sports, and Other

### User Authentication & Profiles
- **ASP.NET Identity**: Secure registration and login system
- **User Profiles**: Each user has a profile page to manage their created events
- **Full Name Requirement**: Users must provide their full name during registration

### Interactive RSVP System
- **Anonymous Viewing**: Anyone can view events and see RSVP counts
- **Authenticated RSVP**: Only logged-in users can RSVP to events
- **RSVP Management**: Users can manage their RSVP status
- **Notifications**: Event creators receive notifications when someone RSVPs

### External API Integration
- **Weather Service**: Integrated with Open-Meteo API to show weather information for event locations
- **Real-time Data**: Weather data is fetched when viewing event details

### Administration
- **Role-based Security**: Admin and User roles
- **User Management**: Admins can manage users and view statistics
- **Orphaned Events**: Handle events created by deleted users
- **Dashboard**: Comprehensive admin dashboard with statistics

## Technology Stack

- **Backend**: ASP.NET Core MVC 8.0
- **Database**: SQL Server LocalDB
- **ORM**: Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **Frontend**: Razor Views with Bootstrap 5
- **External API**: Open-Meteo Weather API

## Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or Visual Studio Code
- SQL Server LocalDB (included with Visual Studio)

## Setup Instructions

### 1. Clone the Repository
```bash
git clone <repository-url>
cd townsquare
```

### 2. Database Setup
The application uses SQL Server LocalDB with Entity Framework Code-First approach.

#### Security Note

While the default LocalDB connection string is included in `appsettings.json`, 
this is acceptable because:
- LocalDB uses Windows Authentication (no passwords)
- The database is local-only (no external access)
- This is a standard development configuration

In a production environment, connection strings would be:
- Stored in environment variables or Azure Key Vault
- Never committed to source control
- Configured differently per environment

**Connection String**: The default connection string is configured for LocalDB:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TownsquareDb;Trusted_Connection=true;MultipleActiveResultSets=true"
}
```

### 3. Run Migrations
The application will automatically run migrations on startup, but you can also run them manually:

```bash
dotnet ef database update --project Townsquare/Townsquare
```

### 4. Seed Data
The application automatically seeds the database with:
- Admin and User roles
- Default admin user (admin@hotmail.com / Admin123!)
- Sample events

### 5. Run the Application
```bash
dotnet run --project Townsquare/Townsquare
```

The application will be available at `https://localhost:5001` or `http://localhost:5000`

## Default Admin Account

- **Email**: admin@hotmail.com
- **Password**: Admin123!
- **Role**: Admin

**Note:** This is for development/testing purposes only. 
In production, admin accounts should be created securely through proper channels.

## Project Structure

```
Townsquare/
├── Controllers/          # MVC Controllers
│   ├── AdminController.cs
│   ├── EventsController.cs
│   ├── ProfileController.cs
│   └── RSVPController.cs
├── Data/                # Database context and migrations
│   ├── ApplicationDbContext.cs
│   ├── DbSeeder.cs
│   └── Migrations/
├── Models/              # Data models
│   ├── Event.cs
│   ├── EventCategory.cs
│   ├── Notification.cs
│   ├── RSVP.cs
│   └── User.cs
├── Services/            # External services
│   └── WeatherService.cs
├── Views/               # Razor views
│   ├── Admin/
│   ├── Events/
    ├── Home/
│   ├── Profile/
│   └── Shared/
└── wwwroot/             # Static files
    ├── css/
    ├── js/
    └── lib/
```

## Database Schema

### Events Table
- `Id` (Primary Key)
- `Title` (Required, Max 120 chars)
- `Description` (Required, Max 4000 chars)
- `StartUtc` (Required, DateTime)
- `Location` (Required, Max 160 chars)
- `Category` (Required, Enum)
- `CreatedById` (Foreign Key to Users, Nullable)
- `CreatedBy` (Navigation Property)

### RSVPs Table
- `Id` (Primary Key)
- `EventId` (Foreign Key to Events)
- `UserId` (Foreign Key to Users)
- `IsGoing` (Boolean, Default: true)
- `CreatedUtc` (DateTime)

### Notifications Table
- `Id` (Primary Key)
- `RecipientUserId` (Foreign Key to Users)
- `EventId` (Foreign Key to Events)
- `Message` (Required, Max 240 chars)
- `IsRead` (Boolean, Default: false)
- `CreatedUtc` (DateTime)

## Security Considerations

- **Sensitive Data**: Never commit connection strings, API keys, or credentials to version control
- **Configuration**: Local configuration is stored in `appsettings.Development.json` (excluded from git)
- **Authentication**: ASP.NET Core Identity provides secure authentication and authorization
- **Authorization**: Role-based access control for admin functions

## External API Integration

### Weather Service (Open-Meteo)
- **API**: https://api.open-meteo.com/v1/forecast
- **Purpose**: Provides weather information for event locations
- **Implementation**: `WeatherService.cs` handles API calls and data transformation
- **Features**: Temperature, humidity, wind speed, weather description, and icons

## Development Workflow

This project follows agile development practices with:
- **Version Control**: Git with GitHub
- **Feature Branches**: At least two feature branches in addition to main
- **Project Management**: GitHub Projects with Product Backlog, User Stories, and Kanban Board
- **Collaborative Development**: Team-based development with regular commits

## Contributing

1. Create a feature branch from main
2. Make your changes
3. Test thoroughly
4. Commit with clear messages
5. Create a pull request
6. Review and merge

## License

This project is developed as part of the "Development of Web Applications" course at Högskolan i Borås.

## Support

For questions or issues, please contact the development team or create an issue in the repository.
