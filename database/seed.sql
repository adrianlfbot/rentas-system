-- =============================================
-- Datos de prueba (Updated v3)
-- =============================================

INSERT INTO Usuarios (Correo, Password, Tipo, Telefono) VALUES
    ('admin@rentas.com', '$2a$11$ZtG7.qKz3qZ3qZ3qZ3qZ3O', 'Propietario', '5551234567'), -- pass: admin123 (hash placeholder)
    ('inquilino1@gmail.com', '$2a$11$ZtG7.qKz3qZ3qZ3qZ3qZ3O', 'Inquilino', '5559876543'),
    ('inquilino2@gmail.com', '$2a$11$ZtG7.qKz3qZ3qZ3qZ3qZ3O', 'Inquilino', '5558765432');

INSERT INTO ContratoLuz (RPU, Nombre, Email, NumeroMedidor, FechaVencimiento, PeriodoEmision) VALUES
    ('RPU-EDIF-01', 'Edificio Central', 'contacto@cfe.mx', 'MED-GEN-01', '2026-03-15', 'Bimestral'),
    ('RPU-DEPTO-A', 'Depto A', 'inquilino1@gmail.com', 'MED-DPT-A', '2026-03-10', 'Bimestral'),
    ('RPU-DEPTO-B', 'Depto B', 'inquilino2@gmail.com', 'MED-DPT-B', '2026-03-12', 'Bimestral');

INSERT INTO ContratoAgua (NumeroInmueble, Nombre, NumeroContrato, FechaVencimiento, PeriodoEmision) VALUES
    ('INM-001', 'Toma General', 'AGUA-001', '2026-03-20', 'Mensual');

INSERT INTO ContratoInternet (NumeroContrato, Nombre, NumeroPagoOXXO, FechaVencimiento, PeriodoEmision) VALUES
    ('INT-001', 'Fibra Optica', 'OXXO-999', '2026-03-05', 'Mensual');

INSERT INTO Ubicaciones (Calle, Numero, Propietario, NumeroPredial, ContratoLuzId, ContratoAguaId, ContratoInternetId) VALUES
    ('Av. Vallarta', '500', 'Adrian LF', 'PRED-500', 1, 1, 1),
    ('La Fragua', 'S/N', 'Adrian LF', 'PENDIENTE', NULL, NULL, NULL);

INSERT INTO Departamento (IDUbicacion, Clave, Descripcion, Cuartos, Banos, MontoRenta, ContratoLuzId, InquilinoCorreo) VALUES
    (1, '101', 'Planta Baja Interior', 2, 1, 5500.00, 2, 'inquilino1@gmail.com'),
    (1, '102', 'Planta Baja Exterior', 2, 1, 5800.00, 3, 'inquilino2@gmail.com');

INSERT INTO NotasDepartamento (DepartamentoId, Texto, UsuarioCreo) VALUES
    (1, 'Se cambió la chapa de la puerta principal', 'admin@rentas.com'),
    (2, 'Pendiente pintar la pared de la sala', 'admin@rentas.com');
