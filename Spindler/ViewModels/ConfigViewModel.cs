﻿using Spindler.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Spindler.ViewModels
{
    public class ConfigViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ICommand AddCommand { get; private set; }

        public ConfigViewModel()
        {

            AddCommand = new Command(async () =>
            {
                await Shell.Current.GoToAsync($"/{nameof(ConfigDetailPage)}?id=-1");
            });
        }
    }
}