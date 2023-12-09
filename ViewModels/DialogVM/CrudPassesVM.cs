using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DicingBlade.Classes.WaferGrid;
using DicingBlade.Views.CutsProcessViews;
using MachineControlsLibrary.CommonDialog;
using static DicingBlade.Views.CutsProcessViews.PassItemView;

namespace DicingBlade.ViewModels.DialogVM
{
    public class CrudPassesVM : CommonDialogResultable<IEnumerable<Pass>>
    {
        private readonly List<Pass> _passes;

        public CrudPassesVM(IEnumerable<Pass> passes)
        {
            _passes = passes.ToList();
            var shares = passes.Select(p => new Share(p.DepthShare, p.DepthShare));
            var firstShare = shares.First();
            var seed = new List<Share>
            {
                firstShare
            };
            var accShares = shares.Skip(1).Aggregate(seed, (acc, cur) =>
            {
                var last = acc.Last();
                var result = new Share(cur.Part, last.Total + cur.Part);
                acc.Add(result);
                return acc;
            });

            var list = accShares.Select(sh=>sh.Total).ToList();

            if(Shift(ref list, list))
            {
                var acc = 0;
                for(var i = 0; i < list.Count; i++)
                {
                    accShares[i] = new Share(list[i] - acc, list[i]);
                    acc = list[i];
                }
                var lastShare = accShares.Last();
                accShares.Add(new Share(100 - lastShare.Total, 100));
                var lastPass = _passes.Last();
                _passes.Add(new()
                {
                    DepthShare = lastShare.Part,
                    FeedSpeed = lastPass.FeedSpeed,
                    PassNumber = lastPass.PassNumber + 1,
                    RPM = lastPass.RPM,
                });
            }

            Shares = new(accShares);
        }


        private bool Shift(ref List<int> source, IEnumerable<int> check)
        {
            if (!check.Any()) return false;
            var index = source.IndexOf(check.Last());
            if (check.Count() == 1)
            {
                if (check.Last() > 5)
                {
                    source[index] = source[index] - 5;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                var skipSeq = check.SkipLast(1);
                if (check.Last() - skipSeq.Last() > 5)
                {

                    source[index] = source[index] - 5;
                    return true;
                }
                else
                {
                    if (Shift(ref source, skipSeq))
                    {
                        source[index] = source[index] - 5;
                        return true;
                    }
                    return false;
                }
            }
        }


        public ObservableCollection<Share> Shares
        {
            get;
            set;
        }
        public override void SetResult()
        {
            var result = _passes.Zip(Shares, (p, sh) => new Pass()
            {
                DepthShare = sh.Part,
                FeedSpeed = p.FeedSpeed,
                PassNumber = p.PassNumber,
                RPM = p.RPM
            });
            SetResult(result);
        }
    }
}
