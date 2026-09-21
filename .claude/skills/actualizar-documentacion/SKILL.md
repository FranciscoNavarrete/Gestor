---
name: actualizar-documentacion
description: Mantiene docs/documentacion-funcional.html y docs/casos-de-uso.html sincronizados con el código. Usar después de agregar, cambiar o sacar una funcionalidad visible para el usuario (pantalla nueva, endpoint nuevo, regla de negocio nueva o modificada), antes de dar la tarea por terminada.
---

# Actualizar documentación de GestorPOS

Este repo tiene dos documentos HTML autocontenidos en `docs/`, pensados para exportar a PDF y
compartir con el dueño del negocio o el equipo — no son notas técnicas para desarrolladores:

- `docs/documentacion-funcional.html` — qué hace el sistema: módulos, pantallas, reglas de negocio,
  accesos/roles, notificaciones, glosario.
- `docs/casos-de-uso.html` — catálogo de casos de uso (actor, precondiciones, flujo principal, flujos
  alternativos, resultado) para las operaciones principales.

## Cuándo correr esto

Después de terminar un cambio funcional (no hace falta para refactors internos, fixes de bugs sin
cambio de comportamiento visible, o cambios de infraestructura/deploy):

- Se agregó, sacó o cambió una pantalla o flujo que un usuario del negocio usa.
- Se agregó, sacó o cambió una regla de negocio (ej: algo que antes se permitía y ahora no, o viceversa).
- Se agregó un endpoint nuevo que corresponde a una funcionalidad nueva (no cada endpoint interno).

## Qué hacer

1. Leé el `README.md` del repo (sección "Endpoints actuales" y "Frontend") — ya debería estar
   actualizado con el cambio; si no lo está, actualizalo primero ahí, es la fuente de verdad.
2. Leé los dos archivos de `docs/` completos antes de tocarlos.
3. Actualizá **solo el contenido que cambió** — no reescribas secciones que siguen siendo correctas.
   - En `documentacion-funcional.html`: agregá/editá el módulo correspondiente en la sección "Módulos
     del sistema", y agregá una regla nueva en "Reglas de negocio" si el cambio introdujo o modificó
     una (con el mismo formato numerado `.rule` que ya usan las demás).
   - En `casos-de-uso.html`: agregá un caso de uso nuevo (`CU-XX`, siguiente número disponible) si el
     cambio agrega un flujo completo nuevo, o editá el existente si cambió un flujo ya documentado.
     Actualizá también el índice (`nav.toc`) si agregaste un caso nuevo.
4. Mantené el estilo visual tal cual está (mismas clases CSS, misma paleta, mismo tono de escritura:
   llano, para el dueño del negocio, sin jerga técnica de desarrollo — nada de "endpoint", "DTO",
   "query filter", etc. en el texto de estos documentos).
5. Actualizá la fecha en el `<div class="meta">` de la portada de cada archivo que hayas tocado
   (formato "mes año", ej. "octubre 2026") — no toques la versión (1.0) salvo que el usuario pida
   explícitamente subirla.
6. Si el cambio no amerita nada nuevo en ninguno de los dos documentos (ej: un fix interno), no
   fuerces una edición — decilo y seguí.

No es necesario abrir los archivos en el navegador para verificar esto salvo que el cambio sea grande
(varios módulos nuevos) — son ediciones de texto puntuales sobre HTML ya validado.
