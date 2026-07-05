namespace GGNet.Scales;

public interface IScale
{
	void Commit(bool grid);

	void Clear();
}