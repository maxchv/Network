﻿using ChatClient.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChatClient
{
    /// <summary>
    /// Interaction logic for MessageControl.xaml
    /// </summary>
    
    public enum ReceiverOrSender
    {
        Receiver,
        Sender
    };
    public partial class MessageControl : UserControl
    {
        public MessageInfo MsgInfo { get; set; }
        public MessageControl()
        {
            InitializeComponent();
            DataContext = this;
            
        }
        public void ApplyStyles(ReceiverOrSender person)
        {
            switch (person)
            {
                case ReceiverOrSender.Receiver:
                    HorizontalAlignment = HorizontalAlignment.Left;
                    Background = Brushes.LightGreen;
                    break;
                case ReceiverOrSender.Sender:
                    HorizontalAlignment = HorizontalAlignment.Right;
                    Background = Brushes.LightBlue;
                    break;
                default:
                    break;
            }
        }
    }
}