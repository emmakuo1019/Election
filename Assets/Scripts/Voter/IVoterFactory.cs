using UnityEngine;

public interface IVoterFactory
{
    // 注意：專案中原本的選民控制腳本為 VoterLogic，而非 VoterController
    VoterLogic CreateVoter(VoterAttribute type, Vector3 position);
}
