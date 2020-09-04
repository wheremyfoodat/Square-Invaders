Imports Helpers
Imports memory
Imports Invaders_Ports

Public Module i8080_module
    Public Class i8080
        Private a, b, c, d, e, h, l As Byte
        Private PC As UShort = 0
        Public cycles As UInteger = 0
        Private SP As UShort = 0
        Private halt As Boolean = False
        Public IME As Boolean = False
        Private carry, sign, auxiliary, parity, zero As Boolean
        Private mem As memory
        Private ports As Invaders_Ports

        Dim file = My.Computer.FileSystem.OpenTextFileWriter("C:\Users\Georgios\Downloads\SI_LOG.txt", False)

        ' Thanks to whoever made this
        Dim OPCODE_CYCLES() As Byte = {
            4, 10, 7, 5, 5, 5, 7, 4, 4, 10, 7, 5, 5, 5, 7, 4,  ' 0
            4, 10, 7, 5, 5, 5, 7, 4, 4, 10, 7, 5, 5, 5, 7, 4,  ' 1
            4, 10, 16, 5, 5, 5, 7, 4, 4, 10, 16, 5, 5, 5, 7, 4,  ' 2
            4, 10, 13, 5, 10, 10, 10, 4, 4, 10, 13, 5, 5, 5, 7, 4,  ' 3
            5, 5, 5, 5, 5, 5, 7, 5, 5, 5, 5, 5, 5, 5, 7, 5,  ' 4
            5, 5, 5, 5, 5, 5, 7, 5, 5, 5, 5, 5, 5, 5, 7, 5,  ' 5
            5, 5, 5, 5, 5, 5, 7, 5, 5, 5, 5, 5, 5, 5, 7, 5,  ' 6
            7, 7, 7, 7, 7, 7, 7, 7, 5, 5, 5, 5, 5, 5, 7, 5,  ' 7
            4, 4, 4, 4, 4, 4, 7, 4, 4, 4, 4, 4, 4, 4, 7, 4,  ' 8
            4, 4, 4, 4, 4, 4, 7, 4, 4, 4, 4, 4, 4, 4, 7, 4,  ' 9
            4, 4, 4, 4, 4, 4, 7, 4, 4, 4, 4, 4, 4, 4, 7, 4,  ' A
            4, 4, 4, 4, 4, 4, 7, 4, 4, 4, 4, 4, 4, 4, 7, 4,  ' B
            5, 10, 10, 10, 11, 11, 7, 11, 5, 10, 10, 10, 11, 17, 7, 11, ' C
            5, 10, 10, 10, 11, 11, 7, 11, 5, 10, 10, 10, 11, 17, 7, 11,  ' D
            5, 10, 10, 18, 11, 11, 7, 11, 5, 5, 10, 4, 11, 17, 7, 11, ' E
            5, 10, 10, 4, 11, 11, 7, 11, 5, 5, 10, 4, 11, 17, 7, 11  ' F
        }

        Public Sub New(ByRef mem_ref, ByRef ports_ref)
            mem = mem_ref
            ports = ports_ref
        End Sub

        Public Property AF() As UShort
            Get
                Return (Convert.ToUInt16(a) << 8) Or (Convert.ToByte(sign) << 7) Or (Convert.ToByte(zero) << 6) Or (Convert.ToByte(auxiliary) << 4) Or (Convert.ToByte(parity) << 2) Or (1 << 1) Or Convert.ToByte(carry)
            End Get

            Set(value As UShort) 'Bits 1, 3 and 5 of the F register aren't overwriteable
                a = value >> 8
                sign = isBitSet(value, 7)
                zero = isBitSet(value, 6)
                auxiliary = isBitSet(value, 4)
                parity = isBitSet(value, 2)
                carry = isBitSet(value, 0)
            End Set

        End Property

        Private Property BC() As UShort
            Get
                Return (Convert.ToUInt16(b) << 8) Or Convert.ToUInt16(c)
            End Get

            Set(value As UShort)
                b = value >> 8
                c = value And &HFF
            End Set
        End Property

        Private Property DE() As UShort
            Get
                Return (Convert.ToUInt16(d) << 8) Or Convert.ToUInt16(e)
            End Get

            Set(value As UShort)
                d = value >> 8
                e = value And &HFF
            End Set
        End Property

        Private Property HL() As UShort
            Get
                Return (Convert.ToUInt16(h) << 8) Or Convert.ToUInt16(l)
            End Get

            Set(value As UShort)
                h = value >> 8
                l = value And &HFF
            End Set
        End Property

        Private Sub push(value As UShort)
            SP -= 2
            mem.writeWord(SP, value)
        End Sub

        Private Function pop() As UShort
            Dim popped_val As UShort = mem.readWord(SP)
            SP += 2
            Return popped_val
        End Function

        Private Sub JMP(opcode As Byte)
            If getConditionFromOpcode(opcode, True) Then
                PC = mem.readWord(PC)
            Else
                PC += 2
            End If
        End Sub

        '$
        Private Sub ADD(operand2 As Byte, Optional cy As Boolean = False)
            Dim result As UShort = a + operand2 + Convert.ToByte(cy)
            auxiliary = (((a And 15) + (operand2 And 15) + Convert.ToByte(cy)) And 16) > 0

            result = result And &HFF
            setSignZeroParity(result)
            carry = result < a
            a = result
        End Sub
        '$
        Private Sub SUB_OP(operand2 As Byte, Optional cy As Boolean = False) ' I implemented it like it's implemented in the ALU, cause I sure as hell ain't going through that flag bs again
            ADD(Not operand2, Not cy)
            carry = Not carry
        End Sub
        '$
        Private Sub ADC(operand2 As Byte)
            ADD(operand2, carry)
        End Sub

        Private Sub SBC(operand2 As Byte)
            SUB_OP(operand2, carry)
        End Sub

        Private Sub AND_OP(operand2 As Byte) ' Fuck you VB.NET keywords
            Dim result As Byte = a And operand2
            setSignZeroParity(result)
            carry = False
            auxiliary = isBitSet(a, 3) Or isBitSet(operand2, 3)
            a = result
        End Sub

        Private Sub XOR_OP(operand2 As Byte) ' Fuck you VB.NET keywords v2
            Dim result As Byte = a Xor operand2
            setSignZeroParity(result)
            carry = False
            auxiliary = False
            a = result
        End Sub

        Private Sub OR_OP(operand2 As Byte) ' Fuck you VB.NET keywords v3
            Dim result As Byte = a Or operand2
            setSignZeroParity(result)
            carry = False
            auxiliary = False
            a = result
        End Sub

        Private Sub CMP(operand2 As Byte)
            Dim result As Byte = a - operand2
            setSignZeroParity(result)
            carry = (result > a)
            auxiliary = (Not (a Xor operand2 Xor result) And &H10) = &H10
        End Sub

        Public Sub CALL_OP(opcode As Byte) ' CALL is a fucking VB.NET keyword. Of course.
            If getConditionFromOpcode(opcode) Then
                push(PC + 2)
                PC = mem.readWord(PC)
            Else
                PC += 2
            End If
        End Sub

        Public Sub interrupt(vector As Byte)
            push(PC)
            IME = False
            PC = vector
            cycles += 11
        End Sub

        Private Sub RET(opcode As Byte)
            If getConditionFromOpcode(opcode) Then
                PC = pop()
            End If
        End Sub

        Private Function getConditionFromOpcode(opcode As Byte, Optional isJMP As Boolean = False) As Boolean
            If opcode And 1 Then
                Return True
            End If

            Dim cond = False
            Select Case ((opcode >> 3) And 7)
                Case 0
                    cond = Not zero
                Case 1
                    cond = zero
                Case 2
                    cond = Not carry
                Case 3
                    cond = carry
                Case 4
                    cond = Not parity
                Case 5
                    cond = parity
                Case 6
                    cond = Not sign
                Case 7
                    cond = sign
            End Select

            If cond And Not isJMP Then
                cycles += 6 ' Conditional instructions take +6 cycles if the cond is met. Except for JMP apparently...
            End If

            Return cond
        End Function

        Private Function getRegisterFromBits(num As Byte) As Byte ' For instruction decoding
            Select Case num
                Case 0
                    Return b
                Case 1
                    Return c
                Case 2
                    Return d
                Case 3
                    Return e
                Case 4
                    Return h
                Case 5
                    Return l
                Case 6
                    Return mem.readByte(HL)
                Case 7
                    Return a
            End Select
        End Function

        Private Sub setSignZeroParity(result As Byte)
            zero = (result = 0)
            sign = (result >> 7) = 1
            Dim setBits = 0

            For i As Integer = 0 To 7
                If isBitSet(result, i) Then
                    setBits += 1
                End If
            Next

            parity = Not ((setBits And 1) = 1)
        End Sub

        Public Function INR(value As Byte) As Byte
            Dim result = value + 1
            setSignZeroParity(result)
            auxiliary = Not (result And &HF)
            Return result
        End Function

        Public Function DCR(value As Byte) As Byte
            Dim result As Byte = value - 1
            setSignZeroParity(result)
            auxiliary = Not ((result And &HF) = &HF)
            Return result
        End Function

        Public Function DAD(value As UShort) As UShort
            Dim result As UShort = HL + value
            carry = HL > result
            Return result
        End Function

        Public Sub MOV(opcode As Byte) ' Decodes operands on the spot
            Dim source As Byte = getRegisterFromBits(opcode And 7)
            Dim dest = (opcode >> 3) And 7

            Select Case dest
                Case 0
                    b = source
                Case 1
                    c = source
                Case 2
                    d = source
                Case 3
                    e = source
                Case 4
                    h = source
                Case 5
                    l = source
                Case 6
                    mem.writeByte(HL, source)
                Case 7
                    a = source
            End Select
        End Sub

        Public Sub executeInstruction()
            Dim opcode As Byte = mem.readByte(PC)
            PC += 1
            cycles += OPCODE_CYCLES(opcode)

            If Not halt Then
                executeOpcode(opcode)
            End If

            '''TODO: Close file in destructor
            'Dim output = String.Format("PC: {0:X4}, AF: {1:X4}, BC: {2:X4}, DE: {3:X4}, HL: {4:X4}, SP: {5:X4}", PC, AF, BC, DE, HL, SP)
            'file.WriteLine(output)

        End Sub

        Public Sub executeOpcode(opcode As Byte)
            Select Case opcode
                Case 0, &H8, &H10, &H18, &H20, &H28, &H30, &H38 ' NOP
                    Return

                Case &H1 ' LXI B
                    BC = mem.readWord(PC)
                    PC += 2

                Case &H2 ' STAX B
                    mem.writeByte(BC, a)

                Case &H3 ' INX B
                    BC += 1

                Case &H4 ' INR B
                    b = INR(b)

                Case &H5 ' DCR B
                    b = DCR(b)

                Case &H6 ' MVI B, d8
                    b = mem.readByte(PC)
                    PC += 1

                Case &H7 ' RLC
                    carry = (a >> 7)
                    a = (a << 1) Or Convert.ToByte(carry)

                Case &H9 ' DAD B
                    HL = DAD(BC)

                Case &HA 'LDAX B
                    a = mem.readByte(BC)

                Case &HB 'DCX B
                    BC -= 1

                Case &HC 'INR C
                    c = INR(c)

                Case &HD 'DCR C
                    c = DCR(c)

                Case &HE 'MVI C
                    c = mem.readByte(PC)
                    PC += 1

                Case &HF 'RRC
                    carry = ((a And 1) = 1)
                    a = (a >> 1) Or (Convert.ToByte(carry) << 7)

                Case &H11 ' LXI D
                    DE = mem.readWord(PC)
                    PC += 2

                Case &H12 ' STAX D
                    mem.writeByte(DE, a)

                Case &H13 ' INX D
                    DE += 1

                Case &H14 ' INR D
                    d = INR(d)

                Case &H15 ' DCR D
                    d = DCR(d)

                Case &H16 ' MVI D
                    d = mem.readByte(PC)
                    PC += 1

                Case &H17 ' RAL
                    Dim old_carry = carry
                    carry = ((a >> 7) = 1)
                    a = (a << 1) Or Convert.ToByte(old_carry)

                Case &H19 ' DAD D
                    HL = DAD(DE)

                Case &H1A ' LDAX D
                    a = mem.readByte(DE)

                Case &H1B ' DCX D
                    DE -= 1

                Case &H1C ' INR E
                    e = INR(e)

                Case &H1D ' DCR E
                    e = DCR(e)

                Case &H1E ' MVI E, d8
                    e = mem.readByte(PC)
                    PC += 1

                Case &H1F ' RAR
                    Dim old_carry = carry
                    carry = ((a And 1) = 1)
                    a = (a >> 1) Or (Convert.ToByte(old_carry) << 7)

                Case &H21 ' LXI H gang
                    HL = mem.readWord(PC)
                    PC += 2

                Case &H22 ' SHLD
                    mem.writeWord(mem.readWord(PC), HL)
                    PC += 2

                Case &H23 ' INX H
                    HL += 1

                Case &H24 ' INR H
                    h = INR(h)

                Case &H25 ' DCR H
                    h = DCR(h)

                Case &H26 ' MVI H, d8
                    h = mem.readByte(PC)
                    PC += 1

                Case &H27 ' DAA OH FUCK OH SHIT

                    Dim old_cy = carry
                    Dim correction As Byte = 0
                    Dim lsb As Byte = a And &HF
                    Dim msb As Byte = a >> 4

                    If auxiliary OrElse lsb > 9 Then
                        correction += &H6
                    End If

                    If carry OrElse msb > 9 OrElse (msb >= 9 AndAlso lsb > 9) Then
                        correction += &H60
                        old_cy = True
                    End If

                    ADD(correction)
                    carry = old_cy
                    setSignZeroParity(a)

                Case &H29 ' DAD H
                    HL = DAD(HL)

                Case &H2A ' LHLD
                    HL = mem.readWord(mem.readWord(PC))
                    PC += 2

                Case &H2B ' DCX H
                    HL -= 1

                Case &H2C ' INR l
                    l = INR(l)

                Case &H2D ' DCR l
                    l = DCR(l)

                Case &H2E ' MVI l, d8
                    l = mem.readByte(PC)
                    PC += 1

                Case &H2F ' CMA
                    a = Not a

                Case &H31 ' LXI SP, a16
                    SP = mem.readWord(PC)
                    PC += 2

                Case &H32 ' STA a16
                    mem.writeByte(mem.readWord(PC), a)
                    PC += 2

                Case &H33 ' INX SP
                    SP += 1

                Case &H34 ' INR M
                    mem.writeByte(HL, INR(mem.readByte(HL)))

                Case &H35 ' DCR M
                    mem.writeByte(HL, DCR(mem.readByte(HL)))

                Case &H36 ' MVI M, d8
                    mem.writeByte(HL, mem.readByte(PC))
                    PC += 1

                Case &H37 ' STC
                    carry = True

                Case &H39 ' DAD SP
                    HL = DAD(SP)

                Case &H3A ' LDA a16
                    a = mem.readByte(mem.readWord(PC))
                    PC += 2

                Case &H3B ' DCX SP
                    SP -= 1

                Case &H3C ' INR A
                    a = INR(a)

                Case &H3D ' DCR A
                    a = DCR(a)

                Case &H3E ' MVI A, d8
                    a = mem.readByte(PC)
                    PC += 1

                Case &H3F ' CMC
                    carry = Not carry

                Case &H40 To &H75, &H77 To &H7F
                    MOV(opcode)

                Case &H76
                    halt = True

                Case &HC0, &HC9, &HD0, &HE0, &HF0, &HC8, &HD8, &HE8, &HF8
                    RET(opcode)

                Case &HC2, &HC3, &HD2, &HE2, &HF2, &HCA, &HDA, &HEA, &HFA
                    JMP(opcode)

                Case &HC4, &HD4, &HE4, &HF4, &HCC, &HCD, &HDC, &HEC, &HFC
                    CALL_OP(opcode)

                Case &H80 To &H87
                    Dim operand2 As Byte = getRegisterFromBits(opcode And 7)
                    ADD(operand2)

                Case &H88 To &H8F
                    Dim operand2 As Byte = getRegisterFromBits(opcode And 7)
                    ADC(operand2)

                Case &H90 To &H97
                    Dim operand2 As Byte = getRegisterFromBits(opcode And 7)
                    SUB_OP(operand2)

                Case &H98 To &H9F
                    Dim operand2 As Byte = getRegisterFromBits(opcode And 7)
                    SBC(operand2)

                Case &HA0 To &HA7
                    Dim operand2 As Byte = getRegisterFromBits(opcode And 7)
                    AND_OP(operand2)

                Case &HA8 To &HAF
                    Dim operand2 As Byte = getRegisterFromBits(opcode And 7)
                    XOR_OP(operand2)

                Case &HB0 To &HB7
                    Dim operand2 As Byte = getRegisterFromBits(opcode And 7)
                    OR_OP(operand2)

                Case &HB8 To &HBF
                    Dim operand2 As Byte = getRegisterFromBits(opcode And 7)
                    CMP(operand2)

                Case &HC6 ' ADI
                    ADD(mem.readByte(PC))
                    PC += 1

                Case &HD6 ' SUI
                    SUB_OP(mem.readByte(PC))
                    PC += 1

                Case &HE6 ' ANI
                    AND_OP(mem.readByte(PC))
                    PC += 1

                Case &HF6 ' ORI
                    OR_OP(mem.readByte(PC))
                    PC += 1

                Case &HCE ' ACI
                    ADC(mem.readByte(PC))
                    PC += 1

                Case &HDE ' SBI
                    SBC(mem.readByte(PC))
                    PC += 1

                Case &HEE ' XRI
                    XOR_OP(mem.readByte(PC))
                    PC += 1

                Case &HFE ' CPI
                    CMP(mem.readByte(PC))
                    PC += 1

                Case &HC1 ' POP B
                    BC = pop()

                Case &HD1 ' POP D
                    DE = pop()

                Case &HE1 ' POP H
                    HL = pop()

                Case &HF1 ' POP PSW
                    AF = pop()

                Case &HC5 ' PUSH B
                    push(BC)

                Case &HD5 ' PUSH D
                    push(DE)

                Case &HE5 ' PUSH H
                    push(HL)

                Case &HF5 ' PUSH PSW
                    push(AF)

                Case &HEB ' EX DE, HL/XCHG
                    Dim tmp = DE
                    DE = HL
                    HL = tmp

                Case &HF9 ' SPHL
                    SP = HL

                Case &HE3 ' XTHL
                    Dim tmp = HL
                    HL = mem.readWord(SP)
                    mem.writeWord(SP, tmp)

                Case &HE9 ' PCHL
                    PC = HL

                Case &HD3 ' OUT
                    ports.PORT_OUT(mem.readByte(PC), a)
                    PC += 1

                Case &HFB ' IE
                    IME = True

                Case &HDB ' IN
                    a = ports.PORT_IN(mem.readByte(PC))
                    PC += 1

                Case Else
                    MessageBox.Show("Unimplemented opcode! Opcode: " & Hex(opcode) & " At PC: " & Hex(PC - 1) & " Cycles: " & cycles)
                    End
            End Select
        End Sub
    End Class
End Module
