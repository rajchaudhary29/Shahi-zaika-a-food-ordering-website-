 var swiper = new Swiper(".mySwiper", {
  loop:true,
      navigation: {
        nextEl: ".next",
        prevEl: ".prev",
      },
    });
    const carticon =document.querySelector('.cart-icon');
    const carttab =document.querySelector('.cart-tab');
    const closebtn = document.querySelector('.close-btn');
    const cardList= document.querySelector('.card-list');
    const cartList = document.querySelector('.cart-list');
    const cardTotal = document.querySelector('.cart-total');
    const cartValue = document.querySelector('.cart-value');
    const hamburger = document.querySelector('.hamburger');
    const mobileMenu =document.querySelector('.mobile-menu');


    carticon.addEventListener('click',()=>carttab.classList.add('cart-tab-active'));
    closebtn.addEventListener('click',()=>carttab.classList.remove('cart-tab-active'));
    hamburger.addEventListener('click',()=>mobileMenu.classList.toggle('.mobile-menu-active'));
    let productList = [];
    let cartProduct=[];

    const updateTotals = () =>{
      let totalPrice = 0;
      let totalQuantity = 0;

      document.querySelectorAll('.item').forEach(item =>{
        
          const quantity = parseInt(item.querySelector('.quantity-value').textContent);
          const price = parseFloat(item.querySelector('.item-total').textContent.replace('$',''));
          totalPrice += price;
          totalQuantity +=quantity
        })

        cardTotal.textContent =`$${totalPrice.toFixed(2)}`;
        cartValue.textContent = totalQuantity;

    }




    const showCards = () =>{
      productList.forEach(product =>{
       const orderCard = document.createElement('div');
       orderCard.classList.add('order-card')
       orderCard.innerHTML =`
      <div class="card-image">
        <img src="${product.images}" />
      </div>
      <h4>${product.name}</h4>
      <h4 class="price">${product.price}</h4>
      <a href="#" class="btn card-btn">Add to cart</a>
            `;
            cardList.appendChild(orderCard);
            const cardBtn =orderCard.querySelector('.card-btn') ;

            cardBtn.addEventListener('click',(e)=>{
              e.preventDefault();
             addTocart(product);
            })   

      })
    }
    const addTocart=(product)=>{



      const existingProduct = cartProduct.find(item =>item.id  ===  product.id);
      if(existingProduct){
        alert('already in your cart');
        return;
      }
      cartProduct.push(product);
      

      let quantity = 1;
      let price = parseFloat(product.price.replace('$',''));



      const cartItem =document.createElement('div');
      cartItem.classList.add('item');
      cartItem.innerHTML = `
           <div class="item-image">
                <img src="${product.images}" alt="">
           </div>
              <div class="detail">
                <h4>${product.name}</h4>
                <h4 class="item-total">${product.price}</h4>
                   </div>
                  <div class="flex">
                       <a href="#" class="quantity-btn minus">
                           <i class="fa-solid fa-minus"></i>
                       </a>
                        <h4 class="quantity-value">${quantity}</h4>
                        <a href="#" class="quantity-btn plus">
                         <i class="fa-solid fa-plus"></i>
                       </a>
                      </div>
      
      `;

    cartList.appendChild(cartItem);
    updateTotals();



    const plusBtn = cartItem.querySelector('.plus');
    const quantityValue = cartItem.querySelector('.quantity-value');
    const itemTotal =cartItem.querySelector('.item-total');
    const minusBtn = cartItem.querySelector('.minus');

    plusBtn.addEventListener('click',(e)=>{
      e.preventDefault();

      quantity++;
      quantityValue.textContent=quantity;
      itemTotal.textContent=`$${price * quantity.toFixed(2)}` ;
      updateTotals();

    })
    minusBtn.addEventListener('click',(e)=>{
      e.preventDefault();
      if(quantity>1){
      quantity--;
      quantityValue.textContent=quantity;
      itemTotal.textContent=`$${price * quantity.toFixed(2)}` ;
      }
      else{
        cartItem.classList.add('slide-out');
        setTimeout(()=>{
        cartItem.remove();
        cartProduct =cartProduct.filter(item=>item.id !== product.id);
        updateTotals();
      
      },300)
      }
      updateTotals();

    })



    
 }



    const initApp = () => {
      
      fetch('products.json')
      .then(res=>res.json())
      .then(data=>{
        productList = data;
        console.log(productList);
        showCards();
     })
     .catch(err => console.err)
    }
    initApp();