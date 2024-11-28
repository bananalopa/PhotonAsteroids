using Fusion;

namespace Asteroids.SharedSimple
{
	enum SpaceshipButtons
	{
		Fire = 0,
	}

	public struct SpaceshipInput : INetworkInput
	{
		public float HorizontalInput;
		public float VerticalInput;
		public NetworkButtons Buttons;
	}
}