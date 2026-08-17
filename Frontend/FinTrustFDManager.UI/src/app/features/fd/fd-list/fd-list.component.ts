import {
  Component,
  OnInit
} from '@angular/core';

import {
  CommonModule
} from '@angular/common';

import {
  Router
} from '@angular/router';

import {
  FDIdentificationService,
  FDLanding
} from '../../../core/services/fd-identification.service';


@Component({
  selector: 'app-fd-list',

  standalone: true,

  imports: [
    CommonModule
  ],

  templateUrl: './fd-list.component.html',

  styleUrl: './fd-list.component.css'
})
export class FDListComponent implements OnInit {


  fdList: FDLanding[] = [];

  loading = false;


  constructor(
    private fdService: FDIdentificationService,

    private router: Router
  ) {}


  ngOnInit(): void {

    this.loadFDs();

  }


  // ==============================
  // LOAD FD LANDING DATA
  // ==============================

  loadFDs(): void {

    this.loading = true;


    this.fdService
      .getLandingData()
      .subscribe({

        next: (data) => {

          this.fdList = data;

          this.loading = false;

        },

        error: (error) => {

          console.error(
            'Error loading FD data',
            error
          );

          this.loading = false;

        }

      });

  }


  // ==============================
  // ADD FD
  // ==============================

  addFD(): void {

    this.router.navigate([
      '/fd/add'
    ]);

  }


  // ==============================
  // OPEN FD
  // ==============================

  openFD(fd: FDLanding): void {

    this.router.navigate([
      '/fd/edit',
      fd.fdId
    ]);

  }


  // ==============================
  // EDIT
  // ==============================

  editFD(fd: FDLanding): void {

    this.router.navigate([
      '/fd/edit',
      fd.fdId
    ]);

  }


  // ==============================
  // CASH FLOW
  // ==============================

  cashFlow(fd: FDLanding): void {

    console.log(
      'Cash Flow FD:',
      fd.fdId
    );

    // Add your Cash Flow route here
    // this.router.navigate([
    //   '/fd/cash-flow',
    //   fd.fdId
    // ]);

  }


  // ==============================
  // DELETE
  // ==============================

  deleteFD(fd: FDLanding): void {

    const confirmed =
      confirm(
        `Are you sure you want to delete ${fd.fdReferenceNo}?`
      );


    if (!confirmed) {

      return;

    }


    this.fdService
      .delete(fd.fdId)
      .subscribe({

        next: () => {

          alert(
            'FD deleted successfully'
          );

          this.loadFDs();

        },

        error: (error) => {

          console.error(
            'Delete failed',
            error
          );

          alert(
            'Unable to delete FD'
          );

        }

      });

  }

}
