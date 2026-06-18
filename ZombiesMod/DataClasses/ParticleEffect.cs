using System.Drawing;
using GTA;
using GTA.Native;

namespace ZombiesMod.DataClasses;

public class ParticleEffect : IHandleable, IDeletable
{
	public int Handle { get; }

	public Color Color
	{
		set
		{
			Function.Call((Hash)0x7F8F65877F88783BuL, new InputArgument[5] { Handle, value.R, value.G, value.B, true });
		}
	}

	internal ParticleEffect(int handle)
	{
		Handle = handle;
	}

	public bool Exists()
	{
		return Function.Call<bool>((Hash)0x74AFEF0D2E1E409BuL, new InputArgument[1] { Handle });
	}

	public void Delete()
	{
		Function.Call((Hash)0xC401503DFE8D53CFuL, new InputArgument[2] { Handle, 1 });
	}
}
