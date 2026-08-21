# PortIA_Core
# Proyecto PortIA Core
# Integrantes
  - Ignacio Andrade
  - Airon Saez
  - Nicolas Soto

Este repositorio contiene el Core de PortIA, el cerebro del sistema, encargado de recibir documentos, coordinar los agentes IA, mediante colas de mensajeria, calcular los riesgos de multas portuarias y emitir alertas en tiempo real al frontend.

# Descripción
## ¿Que hace?
PortIA_Core centraliza la inteligencia de negocio. Sus responsabilidades clave incluyen:
  - Ingesta de documentos: Expone endpoints para recibir PDFs y almacenarlos de forma segura
  - Orquestacion de Agentes: Publica y consume eventos en RabbitMQ para coordinar el trabajo de los microservicios en Python.
  - Motor de reglas y Calculo de riesgo: Toma la "Verdad Documental" (Días Libres extraídos por IA) y la "Verdad Portuaria" (Fecha de arribo real extraída por el servicio), y calcula las fechas límite. Si el contenedor está a menos de 48 horas de vencer, cambia su estado a "CRÍTICO".
  - Comunicación en tiempo real: Envía señales WebSockets al frontend para actualizar tableros sin recargar y prepara correos formales automatizados.

## ¿Que problema resuelve?
El desajuste entre las cuatro "verdades" de la logística marítima (Documental, Marítima, Portuaria y Administrativa). El encargado de Comercio Exterior suele tomar decisiones basadas en PDFs antiguos, mientras el puerto cobra multas basadas en el aterrizaje real del contenedor.

PortIA_Core resuelve esto al:
  - Eliminar el uso de Excel como puente de datos manual.
  - Automatiza la matemática de riesgo.
  - Evitar multas por Demurrage y Detention al mandar notificaciones.

  
  
