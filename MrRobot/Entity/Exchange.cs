using MrRobot.inc;
using MrRobot.Interface;

namespace MrRobot.Entity
{
    public class Exchange : Spisok
    {
		public override string SQL =>
			"SELECT*FROM`_exchange`ORDER BY`id`";
		public override SpisokUnit UnitFieldsFill(SpisokUnit unit, dynamic res)
		{
			unit.Name   = res.GetString("name");
			unit.Prefix = res.GetString("prefix");
			unit.Url    = res.GetString("url");
			return unit;
		}

		public Exchange() : base()
		{
			G.Exchange?.Updated?.Invoke();
			Updated = G.Exchange?.Updated;
			G.Exchange = this;
		}
	}
}


