namespace ZoneEngine.Core.Functions.GameFunctions
{
    using MsgPack;

    using OmniCell.Core.Entities;
    using OmniCell.Enums;
    using OmniCell.Interfaces;

    using ZoneEngine.Core.Combat;

    /// <summary>
    /// CastNano: puts a nano on the character at once, with no cast of its own. The Surgery Clinic's
    /// OnUse ends with CastNano 157490, the implant swap window's buff.
    /// </summary>
    public class castnano : FunctionPrototype
    {
        private FunctionType functionId = FunctionType.CastNano;

        public override FunctionType FunctionId
        {
            get
            {
                return this.functionId;
            }
        }

        public override bool Execute(
            INamedEntity self,
            IEntity caller,
            IInstancedEntity target,
            MessagePackObject[] arguments)
        {
            ICharacter character = (target as ICharacter) ?? (self as ICharacter);
            if ((character == null) || (arguments.Length < 1))
            {
                return false;
            }

            NanoCasting.ApplyWithoutCasting(character, arguments[0].AsInt32());
            return true;
        }
    }
}
