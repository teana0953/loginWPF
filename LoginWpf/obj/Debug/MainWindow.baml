   M S B A M L   `   `   `  ����C  ?LoginWpf, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null#clr-namespace:LoginWpfLoginWpf  5   	       LoginWpf.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   (localclr-namespace:LoginWpf     5   	   -   5          � Title$  
MainWindow��5   	   $	��386��6   $	��525��6)    � 	Resources 5	      %   LoginWpf.StringToImageConverter   StringToImageConverter��%   &	          5
   
   6H   5      .��5      � .+�5   
   �� $
��50 0q�6   .+�5      �� -   5        $��btn_fbLogin��5      $	��150��6(   $��Right=�6N   $��0q�6j   
������ .��5      �� $��logInWithFB.png��6   5      �� 5      -   5        $��btn_goolgeLogin��5      $	��150��6,   $	��0 5q�68   $��Right=�6c   $��0q�6   
������ .��5      �� $+��%btn_google_signin_dark_normal_web.png��6   6I   5      �� 5      -   5        $��profilePhoto��5      $��50��6(   $��50��63   ��6?   �� *	ImagePath+ �� 	Converter# [   �� IsAsync . 6�   �� 5       �� Orientation$ 
Horizontal=�6   $��5q�62   .+�5      �� .��6   ID:6   �� 5      -   5        $��tb_Id��5      $	��5 0q�6'   $	��150��64   ��6@   �� *Id+6V   5      �� 5      $ 
Horizontal=�6   $��5q�62   .+�5      �� .��6   Name:6   �� 5      -   5        $��tb_name��5      $	��5 0q�6)   $	��150��66   ��6B   �� *Name+6Z   5      �� 5      $ 
Horizontal=�6   $��5q�62   .+�5      �� .��6   	Gender:6!   �� 5      -   5        $��	tb_gender��5      $	��5 0q�6+   $	��150��68   ��6D   �� *Gender+6^   5       �� 5!      $ 
Horizontal=�6   $��5q�62   .+�5"      �� .��6   DOB:6   �� 5#      -   5        $��tb_dob��5#      $	��5 0q�6(   $	��150��65   ��6A   �� *Dob+6X   5$      �� 5%      $ 
Horizontal=�6   $��5q�62   .+�5&      �� .��6   
Country:6"   �� 5'      -	   5        $��
tb_country��5'      $	��5 0q�6,   $	��150��69   ��6E   �� *	Country+6`   5(      �� 5)      -
   5        $��
btn_logout��5)      $��Right=�6'   $��5q�6C   .��6g   	Log out6p   5*      5+      5,      