namespace GGNet.Scales;

internal interface IScale
{
	void Commit(bool grid);

	void Clear();
}
