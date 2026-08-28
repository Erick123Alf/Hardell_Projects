CREATE TABLE Asistencias (
    Id INT PRIMARY KEY IDENTITY,
    AlumnoId INT NOT NULL,
    Fecha DATE NOT NULL,
    Presente BIT NOT NULL DEFAULT 0,
    PagoColegiatura BIT NOT NULL DEFAULT 0,
    MesColegiatura NVARCHAR(20),  -- ej: 'Agosto 2026'
    Observaciones NVARCHAR(200),
    FOREIGN KEY (AlumnoId) REFERENCES Alumnos(Id)
);