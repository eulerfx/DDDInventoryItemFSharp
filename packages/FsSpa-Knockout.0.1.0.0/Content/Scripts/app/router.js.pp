(function ($) {
    appFsMvc.App = function( contactsViewModel ) {
        return $.sammy( "#content", function () {
            var self = this;

            this.use( Sammy.Cache );
            this.contactViewModel = contactsViewModel;

            this.renderTemplate = function ( html ) {
                self.$element().html( html );
                ko.applyBindings( self.contactViewModel );
            };

            // display all contacts
            this.get( "#/", function() {
                this.render("/Templates/contactDetail.htm", {}, function ( html ) {
                    self.renderTemplate( html );
                });
            });

            // display the create contacts view
            this.get( "#/create", function() {
                this.render("/Templates/contactCreate.htm", {}, function ( html ) {
                    self.renderTemplate( html );
                });
            });
        });
    };

    $(function () {
        $.getJSON( "/api/contacts", function ( data ) {
            var viewModel = new appFsMvc.ViewModels.ContactsViewModel( data );
            appFsMvc.App( viewModel ).run( "#/" );
        });
    });
})(jQuery);

