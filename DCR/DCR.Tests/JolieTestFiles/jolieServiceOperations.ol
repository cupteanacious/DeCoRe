[BuyerOrdersSeller( request )] {
    order@Seller( request )
}

[SellerConfirmsBuyer( request )] {
    confirm@Buyer( request )
}