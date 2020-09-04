Public Class memory
    Dim ROM = My.Computer.FileSystem.ReadAllBytes("C:/Users/Georgios/Downloads/invaders/invaders.rom")
    Private WRAM(&H3FF) As Byte
    Private VRAM(&H1BFF) As Byte

    Public Function readByte(address As UShort) As Byte
        If address < &H2000 Then
            Return ROM(address)

        ElseIf address < &H2400 Then
            Return WRAM(address And &H3FF)

        ElseIf address < &H4000 Then
            Return VRAM(address - &H2400)

        ElseIf address < &H6000 Then
            Return readByte(address - &H2000)

        Else
            MessageBox.Show("Hol up mf unmapped memory. Address: " & Hex(address))
        End If
    End Function

    Public Function readWord(address As UShort) As UShort
        Return (Convert.ToUInt16(readByte(address + 1)) << 8) Or Convert.ToUInt16(readByte(address))
    End Function

    Public Sub writeByte(address As UShort, value As Byte)

        If address < &H2000 Then
            Return

        ElseIf address < &H2400 Then
            WRAM(address And &H3FF) = value

        ElseIf address < &H4000 Then
            VRAM(address - &H2400) = value

        ElseIf address < &H6000 Then
            writeByte(address - &H2000, value)

        Else
            MessageBox.Show("Hol up mf unmapped memory. Address: " & Hex(address))
            End
        End If
    End Sub

    Public Sub writeWord(address As UShort, value As UShort)
        writeByte(address, value And &HFF)
        writeByte(address + 1, value >> 8)
    End Sub

End Class
