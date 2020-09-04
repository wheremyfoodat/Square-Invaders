Public Module Helpers
    Public Function isBitSet(ByVal number As UShort, ByVal bit As Byte) As Boolean
        Return number And (1 << bit)
    End Function

    Public Function setBit(ByVal number As UShort, ByVal bit As Byte, ByVal status As Boolean) As UShort
        If (Not status = isBitSet(number, bit)) Then
            Return number Xor (1 << bit)
        End If

        Return number
    End Function

    Public Sub playSound(dir As String)
        Dim t = New System.Threading.Thread(Sub()
                                                My.Computer.Audio.Play(dir, AudioPlayMode.BackgroundLoop)
                                                My.Computer.Audio.Stop()
                                            End Sub)
        t.Start()
    End Sub

End Module
