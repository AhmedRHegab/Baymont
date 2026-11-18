/** Phone photos are 3-12 MB; an ID card is perfectly readable by eye at this size, in a few hundred KB. */
const MAX_SIDE = 1800

/** Straightens the photo the way the phone saved it (rotation) and shrinks it to a JPEG. Throws if it is not a readable image. */
export async function prepareImage(file: File): Promise<Blob> {
  let bitmap: ImageBitmap
  try {
    bitmap = await createImageBitmap(file, { imageOrientation: 'from-image' })
  } catch {
    throw new Error('تعذر فتح الصورة. التقط صورة أخرى أو اختر ملف صورة صالحاً.')
  }

  const scale = Math.min(1, MAX_SIDE / Math.max(bitmap.width, bitmap.height))
  const canvas = document.createElement('canvas')
  canvas.width = Math.round(bitmap.width * scale)
  canvas.height = Math.round(bitmap.height * scale)
  const ctx = canvas.getContext('2d')
  if (!ctx) throw new Error('المتصفح لا يدعم معالجة الصور.')
  ctx.drawImage(bitmap, 0, 0, canvas.width, canvas.height)
  bitmap.close()

  const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, 'image/jpeg', 0.88))
  if (!blob) throw new Error('تعذر تجهيز الصورة.')
  return blob
}
