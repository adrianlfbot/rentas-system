-- =============================================
-- Sistema de Control de Rentas - Schema SQLite (Updated v2)
-- =============================================

PRAGMA journal_mode=WAL;
PRAGMA foreign_keys=ON;

-- Usuarios
CREATE TABLE IF NOT EXISTS Usuarios (
    Correo          TEXT PRIMARY KEY,
    Password        TEXT NOT NULL,
    FechaUltimoAcceso DATETIME,
    Tipo            TEXT NOT NULL CHECK(Tipo IN ('Propietario', 'Inquilino')),
    INE             TEXT,
    Telefono        TEXT
);

-- Contrato de Luz
CREATE TABLE IF NOT EXISTS ContratoLuz (
    ID              INTEGER PRIMARY KEY AUTOINCREMENT,
    RPU             TEXT NOT NULL,
    Nombre          TEXT NOT NULL,
    Email           TEXT, -- Nuevo campo
    NumeroMedidor   TEXT,
    FechaVencimiento DATE,
    PeriodoEmision  TEXT CHECK(PeriodoEmision IN ('Semanal', 'Quincenal', 'Mensual', 'Bimestral', 'Semestral', 'Anual'))
);

-- Contrato de Agua
CREATE TABLE IF NOT EXISTS ContratoAgua (
    ID              INTEGER PRIMARY KEY AUTOINCREMENT,
    NumeroInmueble  TEXT NOT NULL,
    Nombre          TEXT NOT NULL,
    NumeroContrato  TEXT,
    FechaVencimiento DATE,
    PeriodoEmision  TEXT CHECK(PeriodoEmision IN ('Semanal', 'Quincenal', 'Mensual', 'Bimestral', 'Semestral', 'Anual'))
);

-- Contrato de Internet
CREATE TABLE IF NOT EXISTS ContratoInternet (
    ID              INTEGER PRIMARY KEY AUTOINCREMENT,
    NumeroContrato  TEXT NOT NULL,
    Nombre          TEXT NOT NULL,
    NumeroPagoOXXO  TEXT,
    FechaVencimiento DATE,
    PeriodoEmision  TEXT CHECK(PeriodoEmision IN ('Semanal', 'Quincenal', 'Mensual', 'Bimestral', 'Semestral', 'Anual'))
);

-- Ubicaciones
CREATE TABLE IF NOT EXISTS Ubicaciones (
    IDUbicacion     INTEGER PRIMARY KEY AUTOINCREMENT,
    Calle           TEXT NOT NULL,
    Numero          TEXT NOT NULL,
    Propietario     TEXT,
    NumeroPredial   TEXT,
    ContratoLuzId   INTEGER REFERENCES ContratoLuz(ID),
    ContratoAguaId  INTEGER REFERENCES ContratoAgua(ID),
    ContratoInternetId INTEGER REFERENCES ContratoInternet(ID)
);

-- Departamento
CREATE TABLE IF NOT EXISTS Departamento (
    ID              INTEGER PRIMARY KEY AUTOINCREMENT,
    IDUbicacion     INTEGER NOT NULL REFERENCES Ubicaciones(IDUbicacion),
    Clave           TEXT NOT NULL,
    Descripcion     TEXT,
    Cuartos         INTEGER DEFAULT 0,
    Banos           INTEGER DEFAULT 0,
    Estacionamiento INTEGER DEFAULT 0,
    Extras          TEXT,
    MontoRenta      REAL NOT NULL DEFAULT 0,
    CuotaAgua       REAL DEFAULT 0,
    ContratoLuzId   INTEGER REFERENCES ContratoLuz(ID), -- Nuevo campo
    DiaVencimiento  INTEGER DEFAULT 1 CHECK(DiaVencimiento BETWEEN 1 AND 31),
    DescripcionPublicacion TEXT,
    InquilinoCorreo TEXT REFERENCES Usuarios(Correo),
    UNIQUE(IDUbicacion, Clave)
);

-- Historial de Inquilinos
CREATE TABLE IF NOT EXISTS HistorialInquilinos (
    ID              INTEGER PRIMARY KEY AUTOINCREMENT,
    DepartamentoId  INTEGER NOT NULL REFERENCES Departamento(ID),
    CorreoInquilino TEXT NOT NULL REFERENCES Usuarios(Correo),
    FechaInicio     DATE NOT NULL,
    FechaFin        DATE
);

-- Cobranza
CREATE TABLE IF NOT EXISTS Cobranza (
    ID              INTEGER PRIMARY KEY AUTOINCREMENT,
    IDUbicacion     INTEGER NOT NULL REFERENCES Ubicaciones(IDUbicacion),
    ClaveDepartamento TEXT NOT NULL,
    Periodo         TEXT NOT NULL,
    FechaCobro      DATE,
    Medio           TEXT,
    Monto           REAL NOT NULL DEFAULT 0
);

-- Tickets
CREATE TABLE IF NOT EXISTS Tickets (
    ID              INTEGER PRIMARY KEY AUTOINCREMENT,
    FechaCreacion   DATETIME NOT NULL DEFAULT (datetime('now')),
    UsuarioCreo     TEXT NOT NULL REFERENCES Usuarios(Correo),
    Prioridad       TEXT NOT NULL CHECK(Prioridad IN ('Alta', 'Media', 'Baja')),
    Descripcion     TEXT NOT NULL,
    Estado          TEXT NOT NULL DEFAULT 'Abierto' CHECK(Estado IN ('Abierto', 'EnProgreso', 'Cerrado')),
    UltimoRecordatorio DATETIME
);

-- Adjuntos
CREATE TABLE IF NOT EXISTS Adjuntos (
    ID              INTEGER PRIMARY KEY AUTOINCREMENT,
    MimeType        TEXT NOT NULL,
    Tipo            TEXT NOT NULL,
    IDPadre         INTEGER NOT NULL,
    Filename        TEXT,
    FilePath        TEXT NOT NULL,
    FechaCreacion   DATETIME NOT NULL DEFAULT (datetime('now'))
);

-- Índices
CREATE INDEX IF NOT EXISTS idx_departamento_ubicacion ON Departamento(IDUbicacion);
CREATE INDEX IF NOT EXISTS idx_departamento_inquilino ON Departamento(InquilinoCorreo);
CREATE INDEX IF NOT EXISTS idx_cobranza_periodo ON Cobranza(Periodo);
