# LMSTrainingBuddy

## Local setup

1. Restore and run the API:
   ```bash
   dotnet run --project LMSTrainingBuddy.API
   ```
2. The API automatically ensures a SQLite database exists at `LMSTrainingBuddy.API/Data/courses.db`.
3. To recreate the database manually, run the SQL in `Database/schema.sql` against the SQLite file.

The seed data stored in the database drives the available courses exposed by the API.
