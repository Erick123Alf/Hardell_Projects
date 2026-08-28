create database Arquimedes;



CREATE TABLE Alumnos (
                                Id INT IDENTITY(1,1) PRIMARY KEY,
                                Nombre VARCHAR(100) NOT NULL,
                                Apellidos VARCHAR(100) NOT NULL,
                                Grado VARCHAR(50),
                                Grupo VARCHAR(50),
                                QRBlob VARBINARY(MAX)
                            );