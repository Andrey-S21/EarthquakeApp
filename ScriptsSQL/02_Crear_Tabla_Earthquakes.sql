USE EarthquakeDB;
GO

CREATE TABLE Earthquakes (
    Id VARCHAR(50) PRIMARY KEY,
    Magnitude FLOAT,
    Place NVARCHAR(255),
    EventTime DATETIME2,
    Latitude FLOAT,
    Longitude FLOAT,
    Depth FLOAT,
    Url NVARCHAR(500)
);