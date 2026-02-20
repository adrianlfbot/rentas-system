#  DESARROLLO DE SISTEMA PARA CONTROL DE RENTA DE DEPARTAMENTOS

Este es un desarrollo para poder llevar el control de varias unidades habitacionales que con tienen multiples departamentos en renta, se encargara de cobranza, tickets de mantenimiento y control de su informacion

## Features

- Debe de tener control de usuario, un correo sera tu usuario y debe loguear cada que un usuario accesa, hay dos tipos de usuario, Propietario que tiene acceso a todos los modulos, incluyendo el de usuarios e inquilino, que solo puede levantar tickets de mantemimiento
- Toolbar para navegar entre modulo
- Upload files para todas las entidades
- Diseño responsivo para todos los dispositivos y navegadores

### Entidades

-  Ubicaciones el lugar donde se ponen los depatamentos
1. IDUbicacion
2. Calle
3. Numero
4. Propietario
5. Numero de Predial
6. Contrato de Luz(este e una relacion a la entidad Contrato de Luz)
7. Contrato de Agua(este e una relacion a la entidad Contrato de Agua)
8. Contrato de Internet(este e una relacion a la entidad Contrato de Internet)

- Contrato de Luz 
1. RPU
2. Nombre
3. Numero de medidor
4. Fecha de Vencimiento
5. Periodo de Emision(semanal, quincenal, mensual, bimestral, semestral, anual)

- Contrato de Agua
1. Numero de Inmueble
2. Nombre
3. Numero de Contrato
4. Fecha de Vencimiento
5. Periodo de Emision

- Contrato dee Internet
1. Numero de Contrato
2. Nombre
3. Numero de pago en OXXO
4. Fecha de Vencimiento
5. Periodo de Emision

- Departamento
1. IDUbicacion es la Ubicacion Padre(Campo para relacionar el dedpartamento con su Ubicacion padre)
2. Clave(Puede ser numero o letra, pero es unico por departamento dentro de Ubicacion)
3. Descripcion
4. Cuartos
5. Baños
6. Estacionamiento
7. Extras
8. Monto de Renta
9. Cuota de Agua
10. Dia de Vencimiento(Normalmente primero de mes o dia 15 del mes)
11. Descripcion para publicacion(este es un texto larho y con emojis)
12. Inquilino(usuario del inquilino que esta actualmente rentando)

- Cobranza
1. IDUbicacion 
2. Clave de   departamento
3. Periodo (Mes y año que se cobro)
3. Fecha de cobro
5. Medio
6. Comprobante(adjunto)
7.  Monto
pagos de renta (fecha, monto, método, comprobante)?


- Usuarios
1. Correo
2. Password
3. Fecha de ultimo acceso
4. Tipo(hay 2 propietario e inquilino)
5. Ine
6. Telefono

- Tickets
1. ID
2. Fecha de creacion
3. Usuario que lo creo
4. Prioridad(Alta,media y baja)
5. Descripcion
6. Esttado (abierto, en progreso, cerrado)

- Adjuntos
1. ID
2. MIME/Type
3. Tipo(entidad a la que pertenece)
4.  IDPadre de la entidad a la que pertenecece


### Notas

Los tickets deben de tener un envio de recordatorios cada 3  dias  al propietario
Los adjuntos se guardan todos en una unica tabla y para saber a que entidad pertenecen se usa el Tipo para saber la tabla y el IDPadre de el registrto al que pertenece.
El CSS debe de ser con Tailwind como supabase.com,  dashboard oscuro con sidebar
Se debe de enviar notttificacion en las fechas de vencimiento y emision
Cuando un inquilino se va, queda su historial historial
Se  necesita un tablero  de cobro, sera una tabla  con 3 columnas,  el departamento(ordenados por ubicaciones), si ya  realiizo el pago  el dia del pago y si se realizo el pago del mes  y año seleccionado, si pago o no pago se representara con un check verde o rojo, se debe de ordenar por ubicacion y despues por departamento


