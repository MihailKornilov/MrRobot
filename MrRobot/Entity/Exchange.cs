using MrRobot.Interface;

namespace MrRobot.Entity
{
    public class Exchange : Spisok
    {
		public override string SQL =>
			"SELECT*FROM`_market`ORDER BY`id`";
		public override SpisokUnit UnitInitSecond(SpisokUnit unit, dynamic res)
		{
			unit.Name   = res.GetString("name");
			unit.Prefix = res.GetString("prefix");
			unit.Url    = res.GetString("url");
			return unit;
		}

        //public Exchange() : base() =>
        //    Updated += () => G.Exchange = this;

		/// <summary>
		/// Единица биржм на основании Prefix
		/// </summary>
		public SpisokUnit UnitOnPrefix(string prefix)
		{
			if (prefix.Length == 0)
				return null; // !!! Сделать возврат пустой биржи

			foreach (var unit in ListAll)
				if (unit.Prefix == prefix)
					return unit;

			return null;
		}
	}
}
