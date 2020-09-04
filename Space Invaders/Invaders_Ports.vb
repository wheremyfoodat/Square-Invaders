Imports helpers
Imports System.Windows.Input

Public Class Invaders_Ports
    ' Read ports
    ' INP0 is not used by the code at all. As such, I have yet to implement it.

    ' INP 1:
    ' bit 0 = CREDIT (1 if deposit)
    ' bit 1 = 2P start (1 if pressed)
    ' bit 2 = 1P start (1 if pressed)
    ' bit 3 = Always 1
    ' bit 4 = 1P shot (1 if pressed)
    ' bit 5 = 1P left (1 if pressed)
    ' bit 6 = 1P right (1 if pressed)
    ' bit 7 = Not connected


    ' INP 2:
    ' bit 0 = DIP3 00 = 3 ships  10 = 5 ships
    ' bit 1 = DIP5 01 = 4 ships  11 = 6 ships
    ' bit 2 = Tilt
    ' bit 3 = DIP6 0 = extra ship at 1500, 1 = extra ship at 1000
    ' bit 4 = P2 shot (1 if pressed)
    ' bit 5 = P2 left (1 if pressed)
    ' bit 6 = P2 right (1 if pressed)
    ' bit 7 = DIP7 Coin info displayed in demo screen 0=ON

    Dim inp1 As Byte = &H8
    Dim inp2 As Byte = 0

    Dim shift_amount As Byte = 0
    Dim shift_register As UShort = 0 ' The shift register is 16 bits cause fuck me

    Public Function PORT_IN(port As Byte) As Byte
        Select Case port
            Case 1 ' INP1
                Dim status As Byte = 0

                If Keyboard.IsKeyDown(Key.Enter) Then
                    status = setBit(status, 0, True) ' COIN
                End If

                If Keyboard.IsKeyDown(Key.B) Then
                    status = setBit(status, 1, True) ' P1 Start
                End If

                If Keyboard.IsKeyDown(Key.A) Then
                    status = setBit(status, 4, True) ' P1 FIRE
                End If

                If Keyboard.IsKeyDown(Key.Left) Then
                    status = setBit(status, 5, True) ' P1 Left
                End If

                If Keyboard.IsKeyDown(Key.Right) Then
                    status = setBit(status, 6, True) ' P1 Right
                End If

                Return status

            Case 2 ' INP2
                Return 0

            Case 3 ' shift output
                Dim result As UShort = (shift_register >> (8 - shift_amount))
                Return (result And &HFF)

            Case 6 ' Watchdog
                Return 0
            Case Else
                ' MessageBox.Show("UNIMPLEMENTED OUTPUT TO PORT! TRIED TO READ PORT " & port)
        End Select
    End Function

    Public Sub PORT_OUT(port As Byte, value As Byte)
        Select Case port

            Case 2
                shift_amount = value And 7

            Case 3, 5 ' Sound registers
                handleSounds(port, value)

            Case 4 ' 16 bit shift register
                shift_register >>= 8
                shift_register = shift_register Or (Convert.ToUInt16(value) << 8)

            Case 6 ' Watchdog
                Return
            Case Else
                ' MessageBox.Show("UNIMPLEMENTED OUTPUT TO PORT! TRIED TO WRITE " & value & " TO PORT " & port)
        End Select
    End Sub

    Public Sub handleSounds(port As Byte, value As Byte)
        If port = 3 Then
            If isBitSet(value, 3) Then
                playSound("C:/Users/Georgios/source/repos/Space Invaders/Space Invaders/sounds/invaders-hit.wav")
            End If
        End If
    End Sub
End Class
