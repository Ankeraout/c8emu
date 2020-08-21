using System;
using System.Collections.Generic;

namespace C8Emu {
    class Chip8 {
        private byte[] regV;
        private ushort regI;
        private ushort regPC;
        private Stack<ushort> stack;
        private byte regDT;
        private byte regST;
        private bool[] keypad;
        private bool[] vram;
        private byte[] ram;
        private Random random;
        private int frameStepCounter;

        public delegate void OnDisplayUpdate(bool[] displayData);
        public event OnDisplayUpdate DisplayUpdate;

        public Chip8() {
            this.regV = new byte[16];
            this.keypad = new bool[16];
            this.vram = new bool[2048];
            this.ram = new byte[4096];
            this.stack = new Stack<ushort>();
            this.random = new Random();
            this.Reset();
        }

        public void Reset() {
            this.regPC = 0x200;
            this.regDT = 0;
            this.regST = 0;
            this.stack.Clear();
            this.regI = 0;
            this.frameStepCounter = 0;

            for(int i = 0; i < 16; i++) {
                this.regV[i] = 0;
                this.keypad[i] = false;
                this.vram[i] = false;
                this.ram[i] = 0;
            }

            for(int i = 16; i < 2048; i++) {
                this.vram[i] = false;
                this.ram[i] = 0;
            }

            for(int i = 2048; i < 4096; i++) {
                this.ram[i] = 0;
            }

            // Font data
            new byte[] {
                0xf0, 0x90, 0x90, 0x90, 0xf0, // 0
                0x20, 0x60, 0x20, 0x20, 0x70, // 1
                0xf0, 0x10, 0xf0, 0x80, 0xf0, // 2
                0xf0, 0x10, 0xf0, 0x10, 0xf0, // 3
                0x90, 0x90, 0xf0, 0x10, 0x10, // 4
                0xf0, 0x80, 0xf0, 0x10, 0xf0, // 5
                0xf0, 0x80, 0xf0, 0x90, 0xf0, // 6
                0xf0, 0x10, 0x20, 0x40, 0x40, // 7
                0xf0, 0x90, 0xf0, 0x90, 0xf0, // 8
                0xf0, 0x90, 0xf0, 0x10, 0xf0, // 9
                0xf0, 0x90, 0xf0, 0x90, 0x90, // A
                0xe0, 0x90, 0xe0, 0x90, 0xe0, // B
                0xf0, 0x80, 0x80, 0x80, 0xf0, // C
                0xe0, 0x90, 0x90, 0x90, 0xe0, // D
                0xf0, 0x80, 0xf0, 0x80, 0xf0, // E
                0xf0, 0x80, 0xf0, 0x80, 0x80  // F
            }.CopyTo(this.ram, 0);
        }

        public void FrameAdvance() {
            for(int i = this.frameStepCounter; i < 15; i++) {
                this.Cycle();
            }
        }

        public void SetKey(int index, bool pressed) {
            this.keypad[index % 16] = pressed;
        }

        public void UpdateDisplay() {
            this.DisplayUpdate(this.vram);
            
            if(this.regST > 0) {
                this.regST--;
            }

            if(this.regDT > 0) {
                this.regDT--;
            }
        }

        public void LoadROM(byte[] buffer) {
            if(buffer.Length > 3232) {
                throw new Exception("ROM file is too big.");
            }

            buffer.CopyTo(this.ram, 512);
        }

        public void Cycle() {
            ushort opcode = this.Fetch();

            try {
                this.Execute(opcode);
            } catch(Exception e) {
                Console.WriteLine("Core state:");
                Console.WriteLine(
                    String.Format(
                        "V0={0:X2} V1={1:X2} V2={2:X2} V3={3:X2} V4={4:X2} V5={5:X2} V6={6:X2} V7={7:X3}",
                        this.regV[0],
                        this.regV[1],
                        this.regV[2],
                        this.regV[3],
                        this.regV[4],
                        this.regV[5],
                        this.regV[6],
                        this.regV[7]
                    )
                );
                Console.WriteLine(
                    String.Format(
                        "V8={0:X2} V9={1:X2} VA={2:X2} VB={3:X2} VC={4:X2} VD={5:X2} VE={6:X2} VF={7:X3}",
                        this.regV[8],
                        this.regV[9],
                        this.regV[10],
                        this.regV[11],
                        this.regV[12],
                        this.regV[13],
                        this.regV[14],
                        this.regV[15]
                    )
                );
                Console.WriteLine(
                    String.Format(
                        "I={0:X3} DT={1:X2} ST={2:X2} PC={3:X3}",
                        this.regI,
                        this.regDT,
                        this.regST,
                        this.regPC
                    )
                );

                throw e;
            }

            this.frameStepCounter++;

            if(this.frameStepCounter == 15) {
                this.frameStepCounter = 0;
                this.UpdateDisplay();
            }
        }

        private byte ReadByte(int address) {
            address &= 0xfff;
            return this.ram[address];
        }
        
        private void WriteByte(int address, int value) {
            address &= 0xfff;
            this.ram[address] = (byte)value;
        }

        private ushort Fetch() {
            ushort opcode = (ushort)((this.ReadByte(this.regPC) << 8) | (this.ReadByte(this.regPC + 1)));
            this.regPC += 2;
            return opcode;
        }

        private void OpcodeCLS() {
            for(int i = 0; i < 2048; i++) {
                this.vram[i] = false;
            }
        }

        private void OpcodeRET() {
            if(!this.stack.TryPop(out this.regPC)) {
                throw new Exception("Attempted to POP with empty stack.");
            }
        }

        private void OpcodeSBR(int address) {
            throw new Exception("Subroutine calls are not implemented.");
        }

        private void OpcodeJMP_nnn(int address) {
            this.regPC = (ushort)address;
        }

        private void OpcodeCALL(int address) {
            this.stack.Push(this.regPC);
            this.regPC = (ushort)address;
        }

        private void OpcodeSKE_Vx_nn(int x, int nn) {
            if(this.regV[x] == nn) {
                this.regPC += 2;
            }
        }

        private void OpcodeSKNE_Vx_nn(int x, int nn) {
            if(this.regV[x] != nn) {
                this.regPC += 2;
            }
        }

        private void OpcodeSKE_Vx_Vy(int x, int y) {
            if(this.regV[x] == this.regV[y]) {
                this.regPC += 2;
            }
        }

        private void OpcodeLD_Vx_nn(int x, int nn) {
            this.regV[x] = (byte)nn;
        }

        private void OpcodeADD_Vx_nn(int x, int nn) {
            this.regV[x] += (byte)nn;
        }

        private void OpcodeUND(int opcode) {
            throw new Exception(String.Format("Unknown opcode {0:X4}", opcode));
        }
        
        private void OpcodeLD_Vx_Vy(int x, int y) {
            this.regV[x] = this.regV[y];
        }

        private void OpcodeOR(int x, int y) {
            this.regV[x] |= this.regV[y];
        }

        private void OpcodeAND(int x, int y) {
            this.regV[x] &= this.regV[y];
        }

        private void OpcodeXOR(int x, int y) {
            this.regV[x] ^= this.regV[y];
        }

        private void OpcodeADD_Vx_Vy(int x, int y) {
            bool carry = (this.regV[x] + this.regV[y] >= 0x100);
            this.regV[x] += this.regV[y];
            this.regV[0xf] = (byte)(carry ? 1 : 0);
        }

        private void OpcodeSUB(int x, int y) {
            bool borrow = this.regV[y] > this.regV[x];
            this.regV[x] -= this.regV[y];
            this.regV[0xf] = (byte)(borrow ? 0 : 1);
        }

        private void OpcodeSHR(int x, int y) {
            byte flag = (byte)(this.regV[y] & 0x01);
            this.regV[x] = (byte)(this.regV[y] >> 1);
            this.regV[0xf] = flag;
        }

        private void OpcodeSUBR(int x, int y) {
            bool borrow = this.regV[x] > this.regV[y];
            this.regV[x] = (byte)(this.regV[y] - this.regV[x]);
            this.regV[0xf] = (byte)(borrow ? 0 : 1);
        }

        private void OpcodeSHL(int x, int y) {
            byte flag = (byte)(this.regV[y] >> 7);
            this.regV[x] = (byte)(this.regV[y] << 1);
            this.regV[0xf] = flag;
        }

        private void OpcodeSKNE_Vx_Vy(int x, int y) {
            if(this.regV[x] != this.regV[y]) {
                this.regPC += 2;
            }
        }

        private void OpcodeLD_I_nnn(int nnn) {
            this.regI = (ushort)nnn;
        }

        private void OpcodeJMP_V0_nnn(int nnn) {
            this.regPC = (ushort)(this.regV[0] + nnn);
        }

        private void OpcodeRND(int x, int nn) {
            this.regV[x] = (byte)(this.random.Next() & nn);
        }

        private void OpcodeDRW(int x, int y, int n) {
            this.regV[0xf] = 0;

            for(int i = 0; i < n; i++) {
                int pixelY = (this.regV[y] + i) % 32;
                byte spriteLine = this.ReadByte(this.regI + i);

                for(int j = 0; j < 8; j++) {
                    int pixelX = (this.regV[x] + j) % 64;
                    bool pixelValue = ((spriteLine >> (7 - j)) & 0x01) == 1;
                    int vramIndex = pixelY * 64 + pixelX;
                    
                    if(pixelValue && this.vram[vramIndex]) {
                        this.regV[0xf] = 1;
                    }

                    this.vram[vramIndex] ^= pixelValue;
                }
            }
        }

        private void OpcodeSKP(int x) {
            if(this.regV[x] < 16 && this.keypad[this.regV[x]]) {
                this.regPC += 2;
            }
        }

        private void OpcodeSKNP(int x) {
            if(this.regV[x] < 16 && !this.keypad[this.regV[x]]) {
                this.regPC += 2;
            }
        }

        private void OpcodeLD_Vx_DT(int x) {
            this.regV[x] = this.regDT;
        }

        private void OpcodeWK(int x) {
            bool found = false;

            for(int i = 0; i < 16; i++) {
                if(this.keypad[i]) {
                    this.regV[x] = (byte)i;
                    found = true;
                    break;
                }
            }

            if(!found) {
                this.regPC -= 2;
            }
        }

        private void OpcodeLD_DT_Vx(int x) {
            this.regDT = this.regV[x];
        }

        private void OpcodeLD_ST_Vx(int x) {
            this.regST = this.regV[x];
        }

        private void OpcodeADD_I_Vx(int x) {
            this.regI += this.regV[x];
        }

        private void OpcodeFNT(int x) {
            this.regI = (ushort)(this.regV[x] * 5);
        }

        private void OpcodeBCD(int x) {
            this.WriteByte(this.regI, this.regV[x] / 100);
            this.WriteByte(this.regI + 1, (this.regV[x] / 10) % 10);
            this.WriteByte(this.regI + 2, this.regV[x] % 10);
        }

        private void opcodeSTM(int x) {
            for(int i = 0; i <= x; i++) {
                this.WriteByte(this.regI + i, this.regV[i]);
            }

            this.regI += (ushort)(x + 1);
        }

        private void opcodeLDM(int x) {
            for(int i = 0; i <= x; i++) {
                this.regV[i] = this.ReadByte(this.regI + i);
            }

            this.regI += (ushort)(x + 1);
        }

        private void Execute(ushort opcode) {
            int nnn = opcode & 0x0fff;
            int nn = opcode & 0x00ff;
            int n = opcode & 0x000f;
            int x = (opcode & 0x0f00) >> 8;
            int y = (opcode & 0x00f0) >> 4;

            switch(opcode >> 12) {
                case 0x0:
                    switch(nnn) {
                        case 0x0e0:
                            this.OpcodeCLS();
                            break;
                        
                        case 0x0ee:
                            this.OpcodeRET();
                            break;

                        default:
                            this.OpcodeSBR(nnn);
                            break;
                    }

                    break;

                case 0x1:
                    this.OpcodeJMP_nnn(nnn);
                    break;
                    
                case 0x2:
                    this.OpcodeCALL(nnn);
                    break;
                    
                case 0x3:
                    this.OpcodeSKE_Vx_nn(x, nn);
                    break;
                    
                case 0x4:
                    this.OpcodeSKNE_Vx_nn(x, nn);
                    break;
                    
                case 0x5:
                    switch(n) {
                        case 0:
                            this.OpcodeSKE_Vx_Vy(x, y);
                            break;

                        default:
                            this.OpcodeUND(opcode);
                            break;
                    }

                    break;
                    
                case 0x6:
                    this.OpcodeLD_Vx_nn(x, nn);
                    break;
                    
                case 0x7:
                    this.OpcodeADD_Vx_nn(x, nn);
                    break;
                    
                case 0x8:
                    switch(opcode & 0xf) {
                        case 0x0:
                            this.OpcodeLD_Vx_Vy(x, y);
                            break;
                        
                        case 0x1:
                            this.OpcodeOR(x, y);
                            break;
                        
                        case 0x2:
                            this.OpcodeAND(x, y);
                            break;
                        
                        case 0x3:
                            this.OpcodeXOR(x, y);
                            break;
                        
                        case 0x4:
                            this.OpcodeADD_Vx_Vy(x, y);
                            break;
                        
                        case 0x5:
                            this.OpcodeSUB(x, y);
                            break;
                        
                        case 0x6:
                            this.OpcodeSHR(x, y);
                            break;
                        
                        case 0x7:
                            this.OpcodeSUBR(x, y);
                            break;
                        
                        case 0xe:
                            this.OpcodeSHL(x, y);
                            break;

                        default:
                            this.OpcodeUND(opcode);
                            break;
                    }

                    break;
                    
                case 0x9:
                    this.OpcodeSKNE_Vx_Vy(x, y);
                    break;
                    
                case 0xa:
                    this.OpcodeLD_I_nnn(nnn);
                    break;
                    
                case 0xb:
                    this.OpcodeJMP_V0_nnn(nnn);
                    break;
                    
                case 0xc:
                    this.OpcodeRND(x, nn);
                    break;
                    
                case 0xd:
                    this.OpcodeDRW(x, y, n);
                    break;
                    
                case 0xe:
                    switch(nn) {
                        case 0x9e:
                            this.OpcodeSKP(x);
                            break;

                        case 0xa1:
                            this.OpcodeSKNP(x);
                            break;

                        default:
                            this.OpcodeUND(opcode);
                            break;
                    }

                    break;
                    
                case 0xf:
                    switch(nn) {
                        case 0x07:
                            this.OpcodeLD_Vx_DT(x);
                            break;
                            
                        case 0x0a:
                            this.OpcodeWK(x);
                            break;
                            
                        case 0x15:
                            this.OpcodeLD_DT_Vx(x);
                            break;
                            
                        case 0x18:
                            this.OpcodeLD_ST_Vx(x);
                            break;
                            
                        case 0x1e:
                            this.OpcodeADD_I_Vx(x);
                            break;
                            
                        case 0x29:
                            this.OpcodeFNT(x);
                            break;
                            
                        case 0x33:
                            this.OpcodeBCD(x);
                            break;
                            
                        case 0x55:
                            this.opcodeSTM(x);
                            break;
                            
                        case 0x65:
                            this.opcodeLDM(x);
                            break;

                        default:
                            this.OpcodeUND(opcode);
                            break;
                    }

                    break;
            }
        }
    }
}
