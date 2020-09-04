Imports SpaceInvaders
Imports System.Threading
Imports System.Drawing
Imports System.IO

Public Class Form1

    Dim t As Thread

    Public Sub New()
        InitializeComponent()
        Dim invaders As SpaceInvaders = New SpaceInvaders(Screen)
        t = New Thread(Sub()
                           While (True)
                               invaders.runFrame()
                           End While
                       End Sub)
        t.SetApartmentState(ApartmentState.STA)
        t.Start()

        Show()
    End Sub

    Protected Overrides Sub Finalize()
        t.Abort()
        End
    End Sub
End Class
