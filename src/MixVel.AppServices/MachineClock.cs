namespace MixVel.AppServices;

public class MachineClock : IClock
{
    public DateTime GetNow() => DateTime.Now;
}