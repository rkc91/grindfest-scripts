using GrindFest;

namespace Scripts
{
    public class Party : AutomaticParty
    {
        public override void OnAllHeroesDied()
        {
            CreateHero("Nocc", "Hero");
        }
    }
}

