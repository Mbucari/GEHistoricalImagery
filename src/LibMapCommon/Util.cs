namespace LibMapCommon;

public class Util
{
	public static int Mod(int value, int modulus)
	{
		var result = value % modulus;
		return result >= 0 ? result : result + modulus;
	}
}
