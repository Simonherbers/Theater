   M S B A M L   `   `   `  ����S  OTheaterControl.Interface, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullY1clr-namespace:TheaterControl.Interface.Converters#TheaterControl.Interface.Converters  5      Q-clr-namespace:TheaterControl.Interface.HelperTheaterControl.Interface.Helper  5      Y1clr-namespace:TheaterControl.Interface.ViewModels#TheaterControl.Interface.ViewModels  5      O,clr-namespace:TheaterControl.Interface.ViewsTheaterControl.Interface.Views  5	      6    0TheaterControl.Interface.Views.ConfigurationView   5      W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      H
Converters1clr-namespace:TheaterControl.Interface.Converters     5      @Helper-clr-namespace:TheaterControl.Interface.Helper     5      H
ViewModels1clr-namespace:TheaterControl.Interface.ViewModels     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      >local,clr-namespace:TheaterControl.Interface.Views     5	      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5
        � 	Resources  5      ?  9TheaterControl.Interface.Converters.TypeToStringConverter   TypeToStringConverter?  9TheaterControl.Interface.Converters.StringToTypeConverter  StringToTypeConverter��%:   &	        &	        5   
   6+     5   
   6+   5      .��5      �  � RowDefinitions 5   
   �� 5      $
��Auto �6    � ColumnDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   .+�5   
   �� ��6   �� *Scenes+ �� SelectedItem 61   �� *SelectedScene+��5      �� 5      .R�5      �� ��6   �� *Name+5      5       � 5&   
   $��1��6    5'      �� 5(      $
��Auto �6    �� 5)      $
��Auto �6    �� 5*      $
��Auto �6    �� 5+      $
��Auto �6    .+�5.      �� $	��500��5/      $	��200��50      $
��Left=�51      ��52      �� *SelectedScene.Devices+��53      �� 54      .R�55      �  56      �� 57   "   $
��Auto �63   �� 58   "   $
��Auto �63   �� 59   "   $
��Auto �63   .+�5;      �� $��0��5<   !   $��0��5=   !   $��50��5>   !   $��50��5?   !   $
��Fill=�5A   !   ��5@   !   �� *DeviceImageUri+5A   !   � 5B      $��1��6#    5C   "   �� 5D   &   $
��Auto �64   �� 5E   &   $
��Auto �64   �� 5F   &   $
��Auto �64   .+�5H   "   �� $��0��5I   %   $��0��5J   %   ��5K   %   �� *Topic+�� 5L   "   {�.  $��1��5N   %   $��0��5O   %    J� KeyUp 5P   %   ;  5TheaterControl.Interface.Helper.EventBindingExtension     Command �� *PublishCommand+��5Q   %   �� *Value+ �� Mode$ TwoWay=� �� UpdateSourceTrigger$ PropertyChanged=��� 5R   "   $��2��6,   $��0��69   ��5S   &   �� 5T   *    �� 
TargetType" ��60   	 �� Triggers	 5U   .   �� 5V   2   
 �� Binding
 6>   �� *ConnectionStatus+ �� Value True6c   .a�5W   6   ��  �� Property � �� �� Value$ 	Connected��6M   �� 5X   6    � ��
 � � �6S   5Y   3   �� 5Z   2   
 6>   �� *ConnectionStatus+	 False6c   .a�5[   6   ��  � ��$ Disconnected��6M   �� 5\   6    � ��
 �  ��6S   5]   3   5`   +   5b   #   5c      5k      5l      5n      5�      5�      5�      