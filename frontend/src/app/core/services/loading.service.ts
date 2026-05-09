import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { ILoadingService } from '../interfaces/api.interface';

@Injectable({
  providedIn: 'root'
})
export class LoadingService implements ILoadingService {
  private _isLoading = new BehaviorSubject<boolean>(false);
  public isLoading$ = this._isLoading.asObservable();

  get isLoading(): boolean {
    return this._isLoading.value;
  }

  showLoading(): void {
    this._isLoading.next(true);
  }

  hideLoading(): void {
    this._isLoading.next(false);
  }
}






