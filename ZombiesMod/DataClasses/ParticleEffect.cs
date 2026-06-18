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
			Function.Call(Hash._0x7F8F65877F88783B, new InputArgument[5] { Handle, value.R, value.G, value.B, true });
		}
	}

	internal ParticleEffect(int handle)
	{
		Handle = handle;
	}

	public bool Exists()
	{
		return Function.Call<bool>(Hash._0x74AFEF0D2E1E409B, new InputArgument[1] { Handle });
	}

	public void Delete()
	{
		Function.Call(Hash._0xC401503DFE8D53CF, new InputArgument[2] { Handle, 1 });
	}
}
