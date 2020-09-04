Imports i8080
Imports memory
Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices

Public Class SpaceInvaders
    Private mem As memory = New memory()
    Private ports As Invaders_Ports = New Invaders_Ports()
    Private cpu As i8080 = New i8080(mem, ports)
    Private Const CYCLES_PER_FRAME = 33333
    Private Const HALF_CYCLES_PER_FRAME = 16666
    Private screen As System.Windows.Forms.PictureBox

    Public Sub New(ByRef screen_ref)
        screen = screen_ref
    End Sub

    Public Sub runFrame()
        While cpu.cycles < HALF_CYCLES_PER_FRAME
            cpu.executeInstruction()
        End While

        ' mid-of-screen IRQ here
        If cpu.IME Then
            cpu.interrupt(&H8)
        End If

        While cpu.cycles < CYCLES_PER_FRAME
            cpu.executeInstruction()
        End While

        If cpu.IME Then
            cpu.interrupt(&H10)
        End If

        cpu.cycles -= CYCLES_PER_FRAME
        Dim VRAM_index = &H2400

        Dim framebuffer As New Bitmap(224, 256)
        Dim imageData = framebuffer.LockBits(New Rectangle(0, 0, framebuffer.Width, framebuffer.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb)
        Dim scan0 As IntPtr = imageData.Scan0
        Dim imageBytes((224 * 256 * 3) - 1) As Byte
        Marshal.Copy(scan0, imageBytes, 0, imageBytes.Length)

        For i As Integer = 0 To (256 * 224 \ 8) - 1
            Dim y = i * 8 \ 256
            Dim base_x = (i * 8) Mod 256
            Dim cur_byte = mem.readByte(VRAM_index + i)

            For bit As Integer = 0 To 7
                Dim px = base_x + bit
                Dim py = y

                Dim is_pixel_lit As Boolean = (((cur_byte >> bit) And 1) = 1)
                Dim temp_x = px
                px = py
                py = -temp_x + 256 - 1
                Dim bufferIndex = ((py * 224) + px) * 3
                If is_pixel_lit Then
                    'framebuffer.SetPixel(px, py, Color.White)
                    imageBytes(bufferIndex) = 255
                    imageBytes(bufferIndex + 1) = 255
                    imageBytes(bufferIndex + 2) = 255
                Else
                    'framebuffer.SetPixel(px, py, Color.Black)
                    imageBytes(bufferIndex) = 0
                    imageBytes(bufferIndex + 1) = 0
                    imageBytes(bufferIndex + 2) = 0
                End If

            Next
        Next

        Marshal.Copy(imageBytes, 0, scan0, imageBytes.Length)
        framebuffer.UnlockBits(imageData)
        'Dim resized_framebuffer = New Bitmap(framebuffer, 448, 512)
        screen.Image = framebuffer
    End Sub
End Class
