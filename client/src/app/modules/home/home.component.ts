import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-home',
  standalone: false,
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  userName = '';
  userRoles: string[] = [];

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    this.userName = user?.fullName || user?.userName || 'User';
    this.userRoles = user?.roles ?? [];
  }

  logout(): void {
    this.authService.logout();
  }
}
