/** Redimensiona y recomprime una imagen en el navegador antes de subirla — una foto sacada con el
 * celular hoy pesa varios MB (muy por arriba del límite de 500 KB del logo), así que sin esto
 * cualquier foto real rebotaba. Conserva PNG (por la transparencia); todo lo demás sale como JPG.
 * Si algo falla (formato no decodificable, etc.) devuelve el archivo original sin tocar. */
export function comprimirImagen(archivo: File, maxDimension = 512, calidad = 0.85): Promise<File> {
  return new Promise((resolve) => {
    const url = URL.createObjectURL(archivo);
    const img = new Image();

    img.onload = () => {
      URL.revokeObjectURL(url);

      const escala = Math.min(1, maxDimension / Math.max(img.width, img.height));
      const ancho = Math.round(img.width * escala);
      const alto = Math.round(img.height * escala);

      const canvas = document.createElement('canvas');
      canvas.width = ancho;
      canvas.height = alto;
      const ctx = canvas.getContext('2d');
      if (!ctx) {
        resolve(archivo);
        return;
      }
      ctx.drawImage(img, 0, 0, ancho, alto);

      const tipoSalida = archivo.type === 'image/png' ? 'image/png' : 'image/jpeg';
      const extension = tipoSalida === 'image/png' ? 'png' : 'jpg';

      canvas.toBlob(
        (blob) => {
          if (!blob) {
            resolve(archivo);
            return;
          }
          resolve(new File([blob], `logo.${extension}`, { type: tipoSalida }));
        },
        tipoSalida,
        tipoSalida === 'image/jpeg' ? calidad : undefined,
      );
    };

    img.onerror = () => {
      URL.revokeObjectURL(url);
      resolve(archivo);
    };

    img.src = url;
  });
}
