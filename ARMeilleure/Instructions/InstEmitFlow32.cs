using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;

using static ARMeilleure.Instructions.InstEmitFlowHelper;
using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit32
    {
        public static void B(ArmEmitterContext context)
        {
            IOpCode32BImm op = (IOpCode32BImm)context.CurrOp;

            if (context.CurrBlock.Branch != null)
            {
                context.Branch(context.GetLabel((ulong)op.Immediate));
            }
            else
            {
                context.StoreToContext();
                context.Return(Const(op.Immediate));
            }
        }

        public static void Bl(ArmEmitterContext context)
        {
            Blx(context, x: false);
        }

        public static void Blx(ArmEmitterContext context)
        {
            Blx(context, x: true);
        }

        private static void Blx(ArmEmitterContext context, bool x)
        {
            IOpCode32BImm op = (IOpCode32BImm)context.CurrOp;

            uint pc = op.GetPc();

            bool isThumb = IsThumb(context.CurrOp);

            uint currentPc = isThumb
                ? pc | 1
                : pc - 4;

            SetIntA32(context, GetBankedRegisterAlias(context.Mode, RegisterAlias.Aarch32Lr), Const(currentPc));

            // If x is true, then this is a branch with link and exchange.
            // In this case we need to swap the mode between Arm <-> Thumb.
            if (x)
            {
                SetFlag(context, PState.TFlag, Const(isThumb ? 0 : 1));
            }

            InstEmitFlowHelper.EmitCall(context, (ulong)op.Immediate);
        }

        public static void Blxr(ArmEmitterContext context)
        {
            IOpCode32BReg op = (IOpCode32BReg)context.CurrOp;

            uint pc = op.GetPc();

            Operand addr = GetIntA32(context, op.Rm);
            Operand bitOne = context.BitwiseAnd(addr, Const(1));
            addr = context.BitwiseOr(addr, Const((int)CallFlag)); // Set call flag.

            bool isThumb = IsThumb(context.CurrOp);

            uint currentPc = isThumb
                ? pc | 1
                : pc - 4;

            SetIntA32(context, GetBankedRegisterAlias(context.Mode, RegisterAlias.Aarch32Lr), Const(currentPc));

            SetFlag(context, PState.TFlag, bitOne);

            context.Return(addr); // Call.
        }

        public static void Bx(ArmEmitterContext context)
        {
            IOpCode32BReg op = (IOpCode32BReg)context.CurrOp;

            context.StoreToContext();

            EmitBxWritePc(context, GetIntA32(context, op.Rm));
        }
    }
}