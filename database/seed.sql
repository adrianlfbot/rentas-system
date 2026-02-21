-- =============================================
-- Datos de prueba
-- =============================================

-- Usuarios (password: "admin123" hasheado con bcrypt placeholder - se hashea en el backend)
INSERT INTO Usuarios (Correo, Password, Tipo, Telefono) VALUES
    ('admin@rentas.com', 'admin123', 'Propietario', '5551234567'),
    ('inquilino1@gmail.com', 'pass123', 'Inquilino', '5559876543'),
    ('inquilino2@gmail.com', 'pass123', 'Inquilino', '5558765432'),
    ('inquilino3@gmail.com', 'pass123', 'Inquilino', '5557654321');

-- Contratos de Luz
INSERT INTO ContratoLuz (RPU, Nombre, NumeroMedidor, FechaVencimiento, PeriodoEmision) VALUES
    ('RPU-001', 'CFE Ubicacion Centro', 'MED-12345', '2026-03-15', 'Bimestral'),
    ('RPU-002', 'CFE Ubicacion Norte', 'MED-67890', '2026-04-01', 'Bimestral');

-- Contratos de Agua
INSERT INTO ContratoAgua (NumeroInmueble, Nombre, NumeroContrato, FechaVencimiento, PeriodoEmision) VALUES
    ('INM-001', 'SAPAS Centro', 'CA-12345', '2026-03-20', 'Mensual'),
    ('INM-002', 'SAPAS Norte', 'CA-67890', '2026-03-25', 'Mensual');

-- Contratos de Internet
INSERT INTO ContratoInternet (NumeroContrato, Nombre, NumeroPagoOXXO, FechaVencimiento, PeriodoEmision) VALUES
    ('INT-001', 'Telmex Centro', 'OXXO-11111', '2026-03-10', 'Mensual'),
    ('INT-002', 'Telmex Norte', 'OXXO-22222', '2026-03-12', 'Mensual');

-- Ubicaciones
INSERT INTO Ubicaciones (Calle, Numero, Propietario, NumeroPredial, ContratoLuzId, ContratoAguaId, ContratoInternetId) VALUES
    ('Av. Juárez', '123', 'Adrian LF', 'PRED-001', 1, 1, 1),
    ('Calle Reforma', '456', 'Adrian LF', 'PRED-002', 2, 2, 2);

-- Departamentos - Ubicacion 1 (Av. Juárez)
INSERT INTO Departamento (IDUbicacion, Clave, Descripcion, Cuartos, Banos, Estacionamiento, Extras, MontoRenta, CuotaAgua, DiaVencimiento, DescripcionPublicacion, InquilinoCorreo) VALUES
    (1, 'A', 'Departamento planta baja', 2, 1, 1, 'Patio trasero', 5500.00, 200.00, 1, '🏠 Depto A - Planta baja, 2 recámaras, 1 baño, estacionamiento y patio 🌿', 'inquilino1@gmail.com'),
    (1, 'B', 'Departamento primer piso', 1, 1, 0, NULL, 4000.00, 150.00, 1, '🏢 Depto B - Primer piso, 1 recámara, 1 baño, excelente ubicación ✨', 'inquilino2@gmail.com');

-- Departamentos - Ubicacion 2 (Calle Reforma)
INSERT INTO Departamento (IDUbicacion, Clave, Descripcion, Cuartos, Banos, Estacionamiento, Extras, MontoRenta, CuotaAgua, DiaVencimiento, DescripcionPublicacion, InquilinoCorreo) VALUES
    (2, '1', 'Departamento amplio', 3, 2, 1, 'Bodega, roof garden', 8000.00, 250.00, 15, '🌟 Depto 1 - Amplio, 3 recámaras, 2 baños, estacionamiento, bodega y roof garden 🏡', 'inquilino3@gmail.com'),
    (2, '2', 'Estudio independiente', 1, 1, 0, NULL, 3500.00, 100.00, 15, '💫 Estudio independiente, ideal para persona sola o pareja 🏠', NULL);

-- Historial de inquilinos
INSERT INTO HistorialInquilinos (DepartamentoId, CorreoInquilino, FechaInicio, FechaFin) VALUES
    (1, 'inquilino1@gmail.com', '2025-06-01', NULL),
    (2, 'inquilino2@gmail.com', '2025-09-01', NULL),
    (3, 'inquilino3@gmail.com', '2025-01-15', NULL);

-- Cobranza - Pagos de enero y febrero 2026
INSERT INTO Cobranza (IDUbicacion, ClaveDepartamento, Periodo, FechaCobro, Medio, Monto) VALUES
    (1, 'A', '2026-01', '2026-01-02', 'Transferencia', 5700.00),
    (1, 'B', '2026-01', '2026-01-03', 'Efectivo', 4150.00),
    (2, '1', '2026-01', '2026-01-16', 'Transferencia', 8250.00),
    (1, 'A', '2026-02', '2026-02-01', 'Transferencia', 5700.00),
    (1, 'B', '2026-02', '2026-02-05', 'Efectivo', 4150.00);
-- Nota: Depto 2-1 no ha pagado febrero, Depto 2-2 está vacío

-- Tickets
INSERT INTO Tickets (UsuarioCreo, Prioridad, Descripcion, Estado) VALUES
    ('inquilino1@gmail.com', 'Alta', 'Fuga de agua en el baño, se está acumulando agua en el piso', 'Abierto'),
    ('inquilino2@gmail.com', 'Baja', 'La puerta del closet no cierra bien', 'EnProgreso'),
    ('inquilino3@gmail.com', 'Media', 'El foco del pasillo está fundido', 'Cerrado');
